using ClientAPI.Services.Concurrency;

namespace ClientAPI.Tests.Concurrency;

public class SingleInflightGuardTests
{
    [Fact]
    public async Task EnterAsync_SerializesConcurrentHolders()
    {
        var guard = new SingleInflightGuard();
        var concurrent = 0;
        var maxConcurrent = 0;
        var sync = new object();

        async Task Worker()
        {
            using (await guard.EnterAsync())
            {
                var current = Interlocked.Increment(ref concurrent);
                lock (sync)
                    maxConcurrent = Math.Max(maxConcurrent, current);
                await Task.Delay(5);
                Interlocked.Decrement(ref concurrent);
            }
        }

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(Worker)));

        Assert.Equal(1, maxConcurrent);
    }

    [Fact]
    public async Task EnterAsync_BlocksSecondCaller_UntilFirstReleases()
    {
        var guard = new SingleInflightGuard();
        var first = await guard.EnterAsync();

        var secondEntered = false;
        var secondTask = Task.Run(async () =>
        {
            using (await guard.EnterAsync())
                Volatile.Write(ref secondEntered, true);
        });

        await Task.Delay(50);
        Assert.False(Volatile.Read(ref secondEntered));

        first.Dispose();

        await secondTask;
        Assert.True(Volatile.Read(ref secondEntered));
    }

    [Fact]
    public async Task ReleaserDisposedTwice_DoesNotOverRelease()
    {
        var guard = new SingleInflightGuard();

        var releaser = await guard.EnterAsync();
        releaser.Dispose();
        releaser.Dispose();

        // Hold the single slot: if the double-dispose had over-released, the count would be
        // corrupted and a second caller could enter concurrently.
        var outer = await guard.EnterAsync();

        var secondEntered = false;
        var secondTask = Task.Run(async () =>
        {
            using (await guard.EnterAsync())
                Volatile.Write(ref secondEntered, true);
        });

        await Task.Delay(50);
        Assert.False(Volatile.Read(ref secondEntered));

        outer.Dispose();
        await secondTask;
        Assert.True(Volatile.Read(ref secondEntered));
    }

    [Fact]
    public async Task EnterAsync_WhenHeld_HonorsCancellation()
    {
        var guard = new SingleInflightGuard();
        using var _ = await guard.EnterAsync();

        using var cts = new CancellationTokenSource();
        var pending = guard.EnterAsync(cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }
}

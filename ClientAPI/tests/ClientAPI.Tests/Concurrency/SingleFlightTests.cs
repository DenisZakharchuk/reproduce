using ClientAPI.Services.Concurrency;

namespace ClientAPI.Tests.Concurrency;

public class SingleFlightTests
{
    [Fact]
    public async Task RunIfNeededAsync_WhenNotSatisfied_RunsAction()
    {
        var singleFlight = new SingleFlight(new SingleInflightGuard());

        var ran = false;
        await singleFlight.RunIfNeededAsync(
            () => false,
            _ => { ran = true; return Task.CompletedTask; });

        Assert.True(ran);
    }

    [Fact]
    public async Task RunIfNeededAsync_WhenAlreadySatisfied_SkipsActionAndNeverEntersGuard()
    {
        var guard = new CountingGuard();
        var singleFlight = new SingleFlight(guard);

        var ran = false;
        await singleFlight.RunIfNeededAsync(
            () => true,
            _ => { ran = true; return Task.CompletedTask; });

        Assert.False(ran);
        Assert.Equal(0, guard.EnterCount);
    }

    [Fact]
    public async Task RunIfNeededAsync_ConcurrentCallers_RunsActionExactlyOnce()
    {
        const int callers = 50;
        var singleFlight = new SingleFlight(new SingleInflightGuard());

        var satisfied = false;
        var invocationCount = 0;
        var actionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseAction = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Action(CancellationToken _)
        {
            Interlocked.Increment(ref invocationCount);
            actionStarted.TrySetResult();
            await releaseAction.Task;
            Volatile.Write(ref satisfied, true);
        }

        var tasks = Enumerable.Range(0, callers)
            .Select(_ => Task.Run(() =>
                singleFlight.RunIfNeededAsync(() => Volatile.Read(ref satisfied), Action)))
            .ToArray();

        // Once the first caller is inside the action, the rest are guaranteed to be contending.
        await actionStarted.Task;
        releaseAction.SetResult();

        await Task.WhenAll(tasks);

        Assert.Equal(1, invocationCount);
        Assert.True(Volatile.Read(ref satisfied));
    }

    [Fact]
    public async Task RunIfNeededAsync_WhenActionThrows_ReleasesGuard()
    {
        var singleFlight = new SingleFlight(new SingleInflightGuard());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            singleFlight.RunIfNeededAsync(
                () => false,
                _ => throw new InvalidOperationException()));

        // The guard must have been released despite the failure, so a later run proceeds.
        var ran = false;
        await singleFlight.RunIfNeededAsync(
            () => false,
            _ => { ran = true; return Task.CompletedTask; });

        Assert.True(ran);
    }

    private sealed class CountingGuard : ISingleInflightGuard
    {
        private int _enterCount;

        public int EnterCount => Volatile.Read(ref _enterCount);

        public Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _enterCount);
            return Task.FromResult<IDisposable>(new NoopReleaser());
        }

        private sealed class NoopReleaser : IDisposable
        {
            public void Dispose() { }
        }
    }
}

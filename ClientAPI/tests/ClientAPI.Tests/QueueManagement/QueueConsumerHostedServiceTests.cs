using ClientAPI.Configuration;
using ClientAPI.Services.QueueManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientAPI.Tests.QueueManagement;

public class QueueConsumerHostedServiceTests
{
    [Fact]
    public async Task InvokesHandlerOncePerMessage_AndCreatesScopePerMessage()
    {
        var processed = new List<int>();
        var gate = new object();
        var scopeCount = 0;
        var remaining = 3;
        var allProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var (queue, host) = BuildTracked(
            handle: (message, _) =>
            {
                lock (gate) processed.Add(message.Id);
                if (Interlocked.Decrement(ref remaining) == 0)
                    allProcessed.TrySetResult();
                return Task.CompletedTask;
            },
            onScope: () => Interlocked.Increment(ref scopeCount));

        await host.StartAsync(CancellationToken.None);
        for (var i = 0; i < 3; i++)
            queue.Writer.TryWrite(new TestMessage(i));
        await allProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await host.StopAsync(CancellationToken.None);

        Assert.Equal(new[] { 0, 1, 2 }, processed.OrderBy(x => x).ToArray());
        Assert.Equal(3, scopeCount);
    }

    [Fact]
    public async Task HandlerThrows_LoopContinuesWithNextMessage()
    {
        var secondProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var (queue, host) = BuildTracked(
            handle: (message, _) =>
            {
                if (message.Id == 1)
                    throw new InvalidOperationException("poison");
                secondProcessed.TrySetResult();
                return Task.CompletedTask;
            });

        await host.StartAsync(CancellationToken.None);
        queue.Writer.TryWrite(new TestMessage(1));
        queue.Writer.TryWrite(new TestMessage(2));
        await secondProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await host.StopAsync(CancellationToken.None);

        Assert.True(secondProcessed.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PerMessageTimeout_CancelsStuckHandler_AndLoopContinues()
    {
        var secondProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var (queue, host) = BuildTracked(
            handle: async (message, ct) =>
            {
                if (message.Id == 1)
                    await Task.Delay(Timeout.Infinite, ct);
                else
                    secondProcessed.TrySetResult();
            },
            perMessageTimeout: TimeSpan.FromMilliseconds(150));

        await host.StartAsync(CancellationToken.None);
        queue.Writer.TryWrite(new TestMessage(1));
        queue.Writer.TryWrite(new TestMessage(2));
        await secondProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await host.StopAsync(CancellationToken.None);

        Assert.True(secondProcessed.Task.IsCompletedSuccessfully);
    }

    private static (QueueChannel<TestMessage> queue, QueueConsumerHostedService<TestMessage> host) BuildTracked(
        Func<TestMessage, CancellationToken, Task> handle,
        Action? onScope = null,
        TimeSpan? perMessageTimeout = null)
    {
        const string key = "tenantA";

        var services = new ServiceCollection();
        services.AddScoped<IMessageHandler<TestMessage>>(_ =>
        {
            onScope?.Invoke();
            return new DelegatingHandler(handle);
        });
        var provider = services.BuildServiceProvider();

        var options = new QueueManagementOptions
        {
            Default = new QueueSettings
            {
                Capacity = 100,
                PerMessageTimeout = perMessageTimeout ?? TimeSpan.FromSeconds(30)
            }
        };
        var monitor = new StaticOptionsMonitor<QueueManagementOptions>(options);
        var queue = new QueueChannel<TestMessage>(key, monitor);
        var host = new QueueConsumerHostedService<TestMessage>(
            key,
            new ChannelConsumer<TestMessage>(queue),
            provider.GetRequiredService<IServiceScopeFactory>(),
            monitor,
            NullLogger<QueueConsumerHostedService<TestMessage>>.Instance);

        return (queue, host);
    }
}

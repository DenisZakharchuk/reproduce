using ClientAPI.Configuration;
using ClientAPI.Services.QueueManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClientAPI.Tests.QueueManagement;

public class ChannelQueueTests
{
    private const string Key = "tenantA";

    private static QueueChannel<TestMessage> CreateQueue(int capacity = 10)
    {
        var options = new QueueManagementOptions { Default = new QueueSettings { Capacity = capacity } };
        return new QueueChannel<TestMessage>(Key, new StaticOptionsMonitor<QueueManagementOptions>(options));
    }

    private static ChannelProducer<TestMessage> CreateProducer(QueueChannel<TestMessage> queue, string key = Key)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton(key, queue);
        var provider = services.BuildServiceProvider();
        var tenant = new TenantContext { QueueKey = key };
        return new ChannelProducer<TestMessage>(provider, tenant, NullLogger<ChannelProducer<TestMessage>>.Instance);
    }

    [Fact]
    public async Task TryProduceAsync_WhenCapacityAvailable_WritesAndReturnsTrue()
    {
        var queue = CreateQueue();
        var producer = CreateProducer(queue);

        var produced = await producer.TryProduceAsync(new TestMessage(1));

        Assert.True(produced);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public async Task TryProduceAsync_AfterQueueCompleted_ReturnsFalse()
    {
        var queue = CreateQueue();
        var producer = CreateProducer(queue);
        queue.Writer.Complete();

        var produced = await producer.TryProduceAsync(new TestMessage(1));

        Assert.False(produced);
    }

    [Fact]
    public async Task ConsumeAsync_YieldsProducedMessagesInOrder()
    {
        var queue = CreateQueue();
        var producer = CreateProducer(queue);
        var consumer = new ChannelConsumer<TestMessage>(queue);

        for (var i = 0; i < 3; i++)
            await producer.TryProduceAsync(new TestMessage(i));
        queue.Writer.Complete();

        var received = new List<int>();
        await foreach (var message in consumer.ConsumeAsync(CancellationToken.None))
            received.Add(message.Id);

        Assert.Equal(new[] { 0, 1, 2 }, received);
    }
}

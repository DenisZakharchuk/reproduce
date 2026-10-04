using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClientAPI.Services.QueueManagement;

public sealed class ChannelProducer<TMessage> : IProducer<TMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ChannelProducer<TMessage>> _logger;

    public ChannelProducer(
        IServiceProvider serviceProvider,
        ITenantContext tenant,
        ILogger<ChannelProducer<TMessage>> logger)
    {
        _serviceProvider = serviceProvider;
        _tenant = tenant;
        _logger = logger;
    }

    public async ValueTask<bool> TryProduceAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        var queueKey = _tenant.QueueKey;
        // The key is a runtime value, so resolve the keyed queue rather than inject it by attribute.
        var queue = _serviceProvider.GetRequiredKeyedService<QueueChannel<TMessage>>(queueKey);

        try
        {
            // WaitToWriteAsync applies backpressure on a full bounded queue and returns
            // false once the queue is completed; TryWrite covers the DropWrite/DropOldest modes.
            while (await queue.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                if (queue.Writer.TryWrite(message))
                {
                    _logger.LogDebug("Enqueued {MessageType} to queue {QueueKey}. Depth: {Depth}.",
                        typeof(TMessage).Name, queueKey, queue.Count);
                    return true;
                }
            }
        }
        catch (ChannelClosedException)
        {
            // Queue completed between the wait and the write.
        }

        _logger.LogWarning("Failed to enqueue {MessageType} to queue {QueueKey}; queue is completed or dropped the message.",
            typeof(TMessage).Name, queueKey);
        return false;
    }
}

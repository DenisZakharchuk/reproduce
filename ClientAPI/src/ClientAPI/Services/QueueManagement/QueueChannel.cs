using System.Threading.Channels;
using ClientAPI.Configuration;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Owns the single bounded <see cref="Channel{T}"/> backing one message type for one queue key.
/// Registered as a keyed singleton (key = tenant/partition) so producer, consumer and hosted
/// service for the same key share one queue.
/// </summary>
public sealed class QueueChannel<TMessage>
{
    private readonly Channel<TMessage> _channel;

    public QueueChannel(string queueKey, IOptionsMonitor<QueueManagementOptions> options)
    {
        Key = queueKey;
        var settings = options.CurrentValue.For(queueKey);
        _channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(settings.Capacity)
        {
            FullMode = settings.FullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>The queue key (tenant/partition) this channel serves.</summary>
    public string Key { get; }

    public ChannelReader<TMessage> Reader => _channel.Reader;

    public ChannelWriter<TMessage> Writer => _channel.Writer;

    /// <summary>Current number of buffered messages, for observability.</summary>
    public int Count => _channel.Reader.Count;
}

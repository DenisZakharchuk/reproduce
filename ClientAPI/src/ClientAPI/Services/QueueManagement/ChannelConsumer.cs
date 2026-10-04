namespace ClientAPI.Services.QueueManagement;

public sealed class ChannelConsumer<TMessage> : IConsumer<TMessage>
{
    private readonly QueueChannel<TMessage> _queue;

    public ChannelConsumer(QueueChannel<TMessage> queue)
    {
        _queue = queue;
    }

    public IAsyncEnumerable<TMessage> ConsumeAsync(CancellationToken cancellationToken) =>
        _queue.Reader.ReadAllAsync(cancellationToken);
}

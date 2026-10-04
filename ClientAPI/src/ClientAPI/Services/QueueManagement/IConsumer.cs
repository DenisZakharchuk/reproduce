namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Reads messages of a single type from the underlying in-process queue.
/// Consumption is streaming: enumerate until the queue is completed and drained.
/// </summary>
public interface IConsumer<out TMessage>
{
    /// <summary>
    /// Streams messages as they arrive. The sequence completes once the queue is
    /// completed and every buffered message has been read.
    /// </summary>
    IAsyncEnumerable<TMessage> ConsumeAsync(CancellationToken cancellationToken);
}

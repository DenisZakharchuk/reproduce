namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Writes messages of a single type onto the underlying in-process queue.
/// One <see cref="IProducer{TMessage}"/> is bound to one queue (channel).
/// </summary>
public interface IProducer<in TMessage>
{
    /// <summary>
    /// Attempts to enqueue <paramref name="message"/>, awaiting free capacity on a bounded queue.
    /// Returns <c>false</c> when the queue is completed (shutting down) or the message was dropped
    /// by the configured full-mode policy.
    /// </summary>
    ValueTask<bool> TryProduceAsync(TMessage message, CancellationToken cancellationToken = default);
}

namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Processes a single dequeued message. Resolved from a fresh DI scope per message,
/// so implementations may depend on scoped services.
/// </summary>
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}

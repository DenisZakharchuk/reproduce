using System.Threading.Channels;

namespace ClientAPI.Configuration;

/// <summary>
/// Configuration for the in-process Channel-based queues.
/// A default applies to every queue; <see cref="Queues"/> overrides per message-type name.
/// </summary>
public class QueueManagementOptions
{
    public QueueSettings Default { get; set; } = new();

    /// <summary>
    /// Per-queue overrides keyed by the message type name (see <c>typeof(TMessage).Name</c>).
    /// </summary>
    public Dictionary<string, QueueSettings> Queues { get; set; } = new();

    public QueueSettings For(string queueName) =>
        Queues.TryGetValue(queueName, out var settings) ? settings : Default;
}

public class QueueSettings
{
    /// <summary>Maximum buffered messages before the full-mode policy applies.</summary>
    public int Capacity { get; set; } = 1000;

    /// <summary>Behaviour when the bounded queue is full.</summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>Per-message processing timeout. Non-positive disables the timeout.</summary>
    public TimeSpan PerMessageTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

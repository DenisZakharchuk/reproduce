using ClientAPI.Services.QueueManagement;
using Microsoft.Extensions.Options;

namespace ClientAPI.Tests.QueueManagement;

internal sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
{
    public StaticOptionsMonitor(T value) => CurrentValue = value;

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed record TestMessage(int Id);

internal sealed class DelegatingHandler : IMessageHandler<TestMessage>
{
    private readonly Func<TestMessage, CancellationToken, Task> _handle;

    public DelegatingHandler(Func<TestMessage, CancellationToken, Task> handle) => _handle = handle;

    public Task HandleAsync(TestMessage message, CancellationToken cancellationToken) =>
        _handle(message, cancellationToken);
}

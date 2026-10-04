using System.Diagnostics;
using ClientAPI.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Dedicated background consumer for one message type. Streams messages from the queue and
/// dispatches each to an <see cref="IMessageHandler{TMessage}"/> resolved from a fresh DI scope,
/// so a singleton host never captures scoped dependencies. A single stuck or failing handler
/// cannot stop the loop: each message runs under a per-message timeout and its exceptions are
/// caught and logged.
/// </summary>
public sealed class QueueConsumerHostedService<TMessage> : IHostedService, IDisposable
{
    private readonly string _queueKey;
    private readonly IConsumer<TMessage> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<QueueManagementOptions> _options;
    private readonly ILogger<QueueConsumerHostedService<TMessage>> _logger;

    private CancellationTokenSource? _stoppingCts;
    private Task? _executeTask;

    public QueueConsumerHostedService(
        string queueKey,
        IConsumer<TMessage> consumer,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<QueueManagementOptions> options,
        ILogger<QueueConsumerHostedService<TMessage>> logger)
    {
        _queueKey = queueKey;
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Linked so an aborted startup also cancels the loop.
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // The loop yields at the first empty read, so this returns without blocking host startup.
        _executeTask = ExecuteAsync(_stoppingCts.Token);

        // Surface any exception thrown before the first await.
        return _executeTask.IsCompleted ? _executeTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executeTask is null)
            return;

        try
        {
            _stoppingCts!.Cancel();
        }
        finally
        {
            // Wait for the loop to drain/unwind, but no longer than the host's shutdown token allows.
            await _executeTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messageType = typeof(TMessage).Name;
        _logger.LogInformation("Queue consumer for {MessageType} [{QueueKey}] started.", messageType, _queueKey);

        try
        {
            await foreach (var message in _consumer.ConsumeAsync(stoppingToken).ConfigureAwait(false))
            {
                await ProcessAsync(message, messageType, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown.
        }

        _logger.LogInformation("Queue consumer for {MessageType} [{QueueKey}] stopped.", messageType, _queueKey);
    }

    private async Task ProcessAsync(TMessage message, string messageType, CancellationToken stoppingToken)
    {
        var timeout = _options.CurrentValue.For(_queueKey).PerMessageTimeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (timeout > TimeSpan.Zero)
            timeoutCts.CancelAfter(timeout);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();

            // Make the scope tenant-aware so the handler and its scoped dependencies see this queue's key.
            if (scope.ServiceProvider.GetService<TenantContext>() is { } tenant)
                tenant.QueueKey = _queueKey;

            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();

            _logger.LogDebug("Processing {MessageType} [{QueueKey}].", messageType, _queueKey);
            await handler.HandleAsync(message, timeoutCts.Token).ConfigureAwait(false);

            _logger.LogInformation("Processed {MessageType} [{QueueKey}] in {ElapsedMs} ms.",
                messageType, _queueKey, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested; let the outer loop unwind.
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing {MessageType} [{QueueKey}] timed out after {ElapsedMs} ms.",
                messageType, _queueKey, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Poison message: log and continue so one failure cannot stop the consumer.
            _logger.LogError(ex, "Handler for {MessageType} [{QueueKey}] failed after {ElapsedMs} ms.",
                messageType, _queueKey, stopwatch.ElapsedMilliseconds);
        }
    }
}

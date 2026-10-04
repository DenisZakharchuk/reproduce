using ClientAPI.Configuration;
using ClientAPI.Services.QueueManagement;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class QueueManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Channel-backed queue for <typeparamref name="TMessage"/> per key: one keyed queue
    /// and one dedicated background consumer per <paramref name="queueKeys"/> entry (e.g. per tenant).
    /// The producer and scoped handler are shared; the producer selects the queue from the current
    /// <see cref="ITenantContext"/> at call time.
    /// </summary>
    public static IServiceCollection AddQueue<TMessage, THandler>(
        this IServiceCollection services, params string[] queueKeys)
        where THandler : class, IMessageHandler<TMessage>
    {
        if (queueKeys is null || queueKeys.Length == 0)
            throw new ArgumentException("At least one queue key is required.", nameof(queueKeys));

        // Shared, cross-queue registrations.
        services.TryAddScoped<TenantContext>();
        services.TryAddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.TryAddScoped<IProducer<TMessage>, ChannelProducer<TMessage>>();
        services.TryAddScoped<IMessageHandler<TMessage>, THandler>();

        foreach (var queueKey in queueKeys)
        {
            var key = queueKey;

            services.AddKeyedSingleton<QueueChannel<TMessage>>(key, (sp, k) =>
                new QueueChannel<TMessage>((string)k!, sp.GetRequiredService<IOptionsMonitor<QueueManagementOptions>>()));

            services.AddKeyedSingleton<IConsumer<TMessage>>(key, (sp, k) =>
                new ChannelConsumer<TMessage>(sp.GetRequiredKeyedService<QueueChannel<TMessage>>(k)));

            // One dedicated hosted service per key.
            services.AddSingleton<IHostedService>(sp => new QueueConsumerHostedService<TMessage>(
                key,
                sp.GetRequiredKeyedService<IConsumer<TMessage>>(key),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptionsMonitor<QueueManagementOptions>>(),
                sp.GetRequiredService<ILogger<QueueConsumerHostedService<TMessage>>>()));
        }

        return services;
    }
}

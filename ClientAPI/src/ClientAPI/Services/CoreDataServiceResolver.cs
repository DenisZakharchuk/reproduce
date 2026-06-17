using ClientAPI.Configuration;
using ClientAPI.Models.Enums;
using ClientAPI.Services.Integrations;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services;

public class CoreDataServiceResolver : ICoreDataServiceResolver
{
    private readonly IOptionsMonitor<CoreDataServiceOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public CoreDataServiceResolver(
        IOptionsMonitor<CoreDataServiceOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public ICoreDataServiceClient Resolve(Osr osr)
    {
        if (!_options.CurrentValue.OsrMappings.TryGetValue(osr, out var serviceType))
            throw new InvalidOperationException($"No Core data service mapping configured for OSR value '{osr}'.");

        return serviceType switch
        {
            CoreDataServiceType.Original => _serviceProvider.GetRequiredKeyedService<ICoreDataServiceClient>("Original"),
            CoreDataServiceType.New => _serviceProvider.GetRequiredKeyedService<ICoreDataServiceClient>("New"),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
        };
    }
}

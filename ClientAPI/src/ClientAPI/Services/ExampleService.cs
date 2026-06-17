using ClientAPI.Configuration;
using ClientAPI.Models;
using ClientAPI.Models.Enums;
using ClientAPI.Services.Integrations;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services;

public class ExampleService : IExampleService
{
    private readonly IOriginalCoreDataServiceClient _originalCoreDataServiceClient;
    private readonly INewCoreDataServiceClient _newCoreDataServiceClient;
    private readonly IDbServiceClient _dbServiceClient;
    private readonly IOptionsMonitor<CoreDataServiceOptions> _coreDataServiceOptions;

    public ExampleService(
        IOriginalCoreDataServiceClient originalCoreDataServiceClient,
        INewCoreDataServiceClient newCoreDataServiceClient,
        IDbServiceClient dbServiceClient,
        IOptionsMonitor<CoreDataServiceOptions> coreDataServiceOptions)
    {
        _originalCoreDataServiceClient = originalCoreDataServiceClient;
        _newCoreDataServiceClient = newCoreDataServiceClient;
        _dbServiceClient = dbServiceClient;
        _coreDataServiceOptions = coreDataServiceOptions;
    }

    public async Task<ExampleResponse> ProcessAsync(ExampleRequest request, CancellationToken cancellationToken = default)
    {
        var mappings = _coreDataServiceOptions.CurrentValue.OsrMappings;
        if (!mappings.TryGetValue(request.Osr, out var serviceType))
            throw new InvalidOperationException($"No Core data service mapping configured for OSR value '{request.Osr}'.");

        string coreData;
        if (serviceType == CoreDataServiceType.Original)
        {
            var response = await _originalCoreDataServiceClient.GetDataAsync(request.EntityId, cancellationToken);
            coreData = response.Value;
        }
        else
        {
            var response = await _newCoreDataServiceClient.GetDataAsync(request.EntityId.ToString(), cancellationToken);
            coreData = response.Payload;
        }

        var dbData = await _dbServiceClient.GetDataAsync(request.EntityId, cancellationToken);

        return new ExampleResponse
        {
            CoreData = coreData,
            DbData = dbData
        };
    }
}

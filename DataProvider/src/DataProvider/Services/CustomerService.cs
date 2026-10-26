using DataProvider.Binary;
using DataProvider.Data;
using DataProvider.Models;

namespace DataProvider.Services;

public class CustomerService : ICustomerService
{
    private readonly IStoredProcedureExecutor _executor;
    private readonly IBinarySerializer _serializer;
    private readonly IBinaryStorage _binaryStorage;

    public CustomerService(
        IStoredProcedureExecutor executor,
        IBinarySerializer serializer,
        IBinaryStorage binaryStorage)
    {
        _executor = executor;
        _serializer = serializer;
        _binaryStorage = binaryStorage;
    }

    public IAsyncEnumerable<Customer> GetCustomersAsync(CancellationToken cancellationToken = default)
        => _executor.QueryUnbufferedAsync<Customer>("usp_GetCustomers", cancellationToken: cancellationToken);

    public async Task<string> InitExportAsync(CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        return await _binaryStorage.WriteAsync(
            expiresAt,
            new BinaryContentWriter<Customer>(_serializer, GetCustomersAsync(cancellationToken)),
            cancellationToken);
    }

    public Task<Stream?> OpenExportAsync(string exportId, CancellationToken cancellationToken = default)
        => _binaryStorage.OpenReadAsync(exportId, cancellationToken);

    public Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
        => _executor.QuerySingleOrDefaultAsync<Customer>(
            "usp_GetCustomerById",
            new { Id = id },
            cancellationToken);

    public Task<int> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
        => _executor.QuerySingleOrDefaultAsync<int>(
            "usp_CreateCustomer",
            new { request.Name, request.Email, request.ConsumtionGroup, request.PaymentData },
            cancellationToken);

    public async Task<PagedResult<CustomerGroup>> GetCustomersGroupedAsync(
        int offset,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var groups = await _executor.QueryAsync<CustomerGroup>(
            "CustomersGrouped",
            new { Offset = offset, PageSize = pageSize, Status = status },
            cancellationToken);

        return new PagedResult<CustomerGroup>(groups, offset, pageSize);
    }
}

using DataProvider.Data;
using DataProvider.Models;

namespace DataProvider.Services;

public class CustomerService : ICustomerService
{
    private readonly IStoredProcedureExecutor _executor;

    public CustomerService(IStoredProcedureExecutor executor)
    {
        _executor = executor;
    }

    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
        => _executor.QueryAsync<Customer>("usp_GetCustomers", cancellationToken: cancellationToken);

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

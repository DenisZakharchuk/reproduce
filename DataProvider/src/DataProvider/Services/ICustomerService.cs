using DataProvider.Models;

namespace DataProvider.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);

    Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerGroup>> GetCustomersGroupedAsync(
        int offset,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);
}

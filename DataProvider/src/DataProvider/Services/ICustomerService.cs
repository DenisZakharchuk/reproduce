using DataProvider.Models;

namespace DataProvider.Services;

public interface ICustomerService
{
    IAsyncEnumerable<Customer> GetCustomersAsync(CancellationToken cancellationToken = default);

    Task<string> InitExportAsync(CancellationToken cancellationToken = default);

    Task<Stream?> OpenExportAsync(string exportId, CancellationToken cancellationToken = default);

    Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerGroup>> GetCustomersGroupedAsync(
        int offset,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);
}

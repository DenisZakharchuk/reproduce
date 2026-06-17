using ClientAPI.Models;

namespace ClientAPI.Services;

public interface IExampleService
{
    Task<ExampleResponse> ProcessAsync(ExampleRequest request, CancellationToken cancellationToken = default);
}

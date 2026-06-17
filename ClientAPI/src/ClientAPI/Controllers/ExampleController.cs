using ClientAPI.Models;
using ClientAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IExampleService _exampleService;

    public ExampleController(IExampleService exampleService)
    {
        _exampleService = exampleService;
    }

    [HttpPost]
    public async Task<ActionResult<ExampleResponse>> ProcessAsync(
        [FromBody] ExampleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _exampleService.ProcessAsync(request, cancellationToken);
        return Ok(result);
    }
}

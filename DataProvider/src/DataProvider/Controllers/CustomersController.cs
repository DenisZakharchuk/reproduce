using DataProvider.Models;
using DataProvider.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private const string ExportRouteName = "CustomerExport";
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetAsync(CancellationToken cancellationToken)
    {
        var customers = new List<Customer>();
        await foreach (var customer in _customerService.GetCustomersAsync(cancellationToken)
            .WithCancellation(cancellationToken))
        {
            customers.Add(customer);
        }

        return Ok(customers);
    }

    [HttpGet("export/init")]
    [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
    public async Task<IActionResult> InitExportAsync(    CancellationToken cancellationToken)
    {
    var exportId = await _customerService.InitExportAsync(cancellationToken);
    var exportUrl = Url.RouteUrl(ExportRouteName, new { exportId });

    if (exportUrl is null)
        throw new InvalidOperationException("Unable to generate the customer export URL.");

        return RedirectPreserveMethod(exportUrl);
    }

    [HttpGet("export/{exportId}", Name = ExportRouteName)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAsync(string exportId, CancellationToken cancellationToken)
    {
        var stream = await _customerService.OpenExportAsync(exportId, cancellationToken);
        return stream is null
            ? NotFound()
            : File(stream, "application/gzip", $"customers-{exportId}.jsonl.gz");
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("grouped")]
    public async Task<ActionResult<PagedResult<CustomerGroup>>> GetGroupedAsync(
        [FromQuery] int offset = 0,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (offset < 0)
            return BadRequest("offset must be zero or greater.");
        if (pageSize is < 1 or > 200)
            return BadRequest("pageSize must be between 1 and 200.");

        var page = await _customerService.GetCustomersGroupedAsync(offset, pageSize, status, cancellationToken);
        return Ok(page);
    }

    [HttpPost]
    public async Task<ActionResult> CreateAsync(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _customerService.CreateCustomerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id }, null);
    }
}

using DataProvider.Models;
using DataProvider.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetCustomersAsync(cancellationToken);
        return Ok(customers);
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

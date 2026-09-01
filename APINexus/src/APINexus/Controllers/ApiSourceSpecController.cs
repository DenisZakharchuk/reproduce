using System.Security.Claims;
using APINexus.Models;
using APINexus.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APINexus.Controllers;

[ApiController]
[Route("api/sources")]
[Authorize]
public class ApiSourceSpecController : ControllerBase
{
    private readonly IApiSourceSpecService _specService;

    public ApiSourceSpecController(IApiSourceSpecService specService)
    {
        _specService = specService;
    }

    [HttpGet("{id}/openapi.json")]
    public async Task<IActionResult> GetSpecAsync(string id, CancellationToken cancellationToken)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var result = await _specService.GetRewrittenSpecAsync(id, roles, cancellationToken);

        return result.Status switch
        {
            ApiSourceSpecStatus.Ok => Content(result.Json!, "application/json"),
            ApiSourceSpecStatus.NotFound => NotFound(),
            ApiSourceSpecStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status502BadGateway)
        };
    }
}

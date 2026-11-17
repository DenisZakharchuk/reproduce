using DataProvider.Models;
using DataProvider.Models.Enums;
using DataProvider.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataProvider.Controllers;

[ApiController]
[Route("api/sync-locks")]
public class SyncLocksController : ControllerBase
{
    private readonly ISyncLockService _syncLockService;

    public SyncLocksController(ISyncLockService syncLockService)
    {
        _syncLockService = syncLockService;
    }

    [HttpPost("acquire")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> AcquireAsync(
        [FromBody] AcquireSyncLockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _syncLockService.AcquireAsync(request, cancellationToken);
        return Ok(result.Acquired);
    }

    [HttpPost("release")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> ReleaseAsync(
        [FromBody] ReleaseSyncLockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not (SyncLockStatus.Done or SyncLockStatus.Failed))
            return BadRequest("Status must be Done or Failed.");

        var result = await _syncLockService.ReleaseAsync(request, cancellationToken);
        return Ok(result.Released);
    }
}

using CommerceManager.Infrastructure.Integrations.TSoft;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/tsoft")]
public sealed class TSoftSyncController(TSoftCatalogSync sync) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var result = await sync.SyncAsync(cancellationToken);
        return Ok(result);
    }
}

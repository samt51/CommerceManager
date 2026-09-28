using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/products")]
public sealed class TrendyolProductsController(
    IMediator mediator,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("sync")]
    public async Task<ActionResult<TrendyolProductSyncResult>> Sync(
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress is not { } remoteIp ||
            !System.Net.IPAddress.IsLoopback(remoteIp))
        {
            return NotFound();
        }

        var result =
            await mediator.Send(
                new SyncTrendyolProducts(),
                cancellationToken);

        return Ok(result);
    }
}

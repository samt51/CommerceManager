using System.Net;
using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/orders/sync")]
public sealed class TrendyolSyncController(
    ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress is not { } address ||
            !IPAddress.IsLoopback(address)) return NotFound();
        return Ok(await sender.Send(new SyncTrendyolOrders(50, 20), cancellationToken));
    }
}

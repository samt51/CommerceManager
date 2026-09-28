using System.Net;
using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/orders/backfill")]
public sealed class TrendyolBackfillController(
    ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Backfill([FromQuery] int days = 14,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress is not { } address ||
            !IPAddress.IsLoopback(address)) return NotFound();
        if (days is < 1 or > 90) return BadRequest("days must be between 1 and 90.");
        return Ok(await sender.Send(new BackfillTrendyolOrders(days), cancellationToken));
    }
}

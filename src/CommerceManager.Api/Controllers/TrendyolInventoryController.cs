using System.Net;
using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/inventory")]
public sealed class TrendyolInventoryController(
    ISender sender,
    IWebHostEnvironment environment)
    : ControllerBase
{
    private bool IsLocalDevelopment =>
        environment.IsDevelopment() &&
        HttpContext.Connection.RemoteIpAddress is { } address &&
        IPAddress.IsLoopback(address);

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        CancellationToken cancellationToken)
    {
        if (!IsLocalDevelopment)
            return NotFound();

        var result = await sender.Send(
            new SyncTrendyolInventory(),
            cancellationToken);

        return Ok(result);
    }
}

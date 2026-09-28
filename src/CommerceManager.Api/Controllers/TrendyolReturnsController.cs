using System.Net;
using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/returns")]
public sealed class TrendyolReturnsController(
    ISender sender,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress
                is not { } address ||
            !IPAddress.IsLoopback(address))
            return NotFound();

        var result = await sender.Send(
            new SyncTrendyolReturns(days),
            cancellationToken);

        return Ok(result);
    }


    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] int days = 30,
        [FromQuery] int page = 0,
        [FromQuery] int size = 50,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress
                is not { } address ||
            !IPAddress.IsLoopback(address))
            return NotFound();

        var result = await sender.Send(
            new ReadTrendyolReturns(
                days,
                page,
                size),
            cancellationToken);

        return Ok(result);
    }
}

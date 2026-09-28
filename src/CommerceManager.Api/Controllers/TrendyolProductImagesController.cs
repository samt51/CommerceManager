using System.Net;
using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/products/images")]
public sealed class TrendyolProductImagesController(
    ISender sender,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("backfill")]
    public async Task<ActionResult<TrendyolProductImageBackfillResult>> Backfill(
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress is not { } address ||
            !IPAddress.IsLoopback(address))
        {
            return NotFound();
        }

        var result = await sender.Send(
            new BackfillTrendyolProductImages(),
            cancellationToken);

        return Ok(result);
    }
}

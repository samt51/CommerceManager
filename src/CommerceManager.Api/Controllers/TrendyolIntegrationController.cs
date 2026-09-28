using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol")]
public sealed class TrendyolIntegrationController(
    ISender sender,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("orders/preview")]
    public async Task<IActionResult> Preview(
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var result = await sender.Send(
            new GetTrendyolOrderPreview(), cancellationToken);
        return Ok(result);
    }
}

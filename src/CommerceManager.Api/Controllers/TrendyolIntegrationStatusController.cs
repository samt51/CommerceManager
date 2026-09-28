using CommerceManager.Application.Integrations.Trendyol;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/trendyol/status")]
public sealed class TrendyolIntegrationStatusController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
var result = await sender.Send(
            new GetTrendyolIntegrationStatus(),
            cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}

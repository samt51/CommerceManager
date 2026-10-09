using CommerceManager.Application.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetBusinessOverview(), cancellationToken));
    }

    [HttpGet("trendyol")]
    public async Task<IActionResult> Trendyol(
        [FromQuery] string period = "this-month",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetTrendyolDashboard(period),
            cancellationToken);

        return Ok(result);
    }
}

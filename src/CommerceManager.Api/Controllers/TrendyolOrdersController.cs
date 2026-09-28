using CommerceManager.Application.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/orders/trendyol")]
public sealed class TrendyolOrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 0,
        [FromQuery] int size = 25, [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
if (page < 0 || size is < 1 or > 100 || page > (int.MaxValue / size))
            return BadRequest("page must be >= 0 and size between 1 and 100.");
        return Ok(await sender.Send(new ListTrendyolOrders(page, size, status),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id,
        CancellationToken cancellationToken)
    {
var result = await sender.Send(new GetTrendyolOrder(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}

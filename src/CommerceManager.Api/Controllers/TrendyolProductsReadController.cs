using CommerceManager.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/products/trendyol")]
public sealed class TrendyolProductsReadController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
if (page < 0 ||
            size is < 1 or > 100 ||
            page > int.MaxValue / size)
        {
            return BadRequest(
                "page must be >= 0 and size between 1 and 100.");
        }

        return Ok(
            await sender.Send(
                new ListTrendyolProducts(
                    page,
                    size,
                    search),
                cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken = default)
    {
var product = await sender.Send(
            new GetTrendyolProduct(id),
            cancellationToken);

        if (product is null)
            return NotFound();

        return Ok(product);
    }

}

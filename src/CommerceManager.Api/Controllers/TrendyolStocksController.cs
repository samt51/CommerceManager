using CommerceManager.Application.Inventory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/stocks/trendyol")]
public sealed class TrendyolStocksController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? stockStatus = null,
        CancellationToken cancellationToken = default)
    {
if (page < 0 ||
            size is < 1 or > 100 ||
            page > int.MaxValue / size)
        {
            return BadRequest(
                "page must be >= 0 and size between 1 and 100.");
        }

        var validStatuses = new[]
        {
            "all",
            "instock",
            "lowstock",
            "outofstock"
        };

        if (!string.IsNullOrWhiteSpace(stockStatus) &&
            !validStatuses.Contains(
                stockStatus.Trim().ToLowerInvariant()))
        {
            return BadRequest(
                "stockStatus must be all, inStock, lowStock or outOfStock.");
        }

        var result =
            await sender.Send(
                new ListTrendyolStocks(
                    page,
                    size,
                    search,
                    stockStatus),
                cancellationToken);

        return Ok(result);
    }
}

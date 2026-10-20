using CommerceManager.Infrastructure.Integrations.TSoft;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/orders/website")]
public sealed class WebsiteOrdersController(TSoftOrderReader orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        CancellationToken cancellationToken = default)
    {
        if (page < 0 || size is < 1 or > 100)
            return BadRequest();

        var rows = await orders.OrdersAsync(cancellationToken);
        var pageRows = rows.Skip(page * size).Take(size);
        return Ok(new
        {
            page,
            size,
            total = rows.Count,
            items = pageRows.Select(x => new
            {
                id = Guid.Empty,
                externalOrderId = x.Code,
                orderNumber = x.Code,
                status = x.Status,
                orderedAt = x.OrderedAt,
                totalAmount = x.Total,
                itemCount = x.ItemCount,
                firstProductName = x.ProductName,
                firstProductImageUrl = x.ImageUrl
            })
        });
    }
}

[ApiController]
[Route("api/returns/website")]
public sealed class WebsiteReturnsController(TSoftOrderReader orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        CancellationToken cancellationToken = default)
    {
        if (page < 0 || size is < 1 or > 100)
            return BadRequest();

        var rows = await orders.ReturnsAsync(cancellationToken);
        var pageRows = rows.Skip(page * size).Take(size);
        return Ok(new
        {
            page,
            size,
            total = rows.Count,
            items = pageRows.Select(x => new
            {
                id = Guid.Empty,
                externalReturnId = x.OrderCode,
                orderId = Guid.Empty,
                orderNumber = x.OrderCode,
                channel = 2,
                barcode = x.Barcode,
                productName = x.ProductName,
                imageUrl = x.ImageUrl,
                quantity = x.Quantity,
                status = 2,
                stockApplied = false,
                externalLastModifiedAt = x.OrderedAt,
                createdAt = x.OrderedAt
            })
        });
    }
}

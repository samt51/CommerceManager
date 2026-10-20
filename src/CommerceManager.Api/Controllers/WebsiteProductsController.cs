using CommerceManager.Infrastructure.Integrations.TSoft;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/products/website")]
public sealed class WebsiteProductsController(TSoftProductReader products) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await products.ReadAsync(cancellationToken);
        return Ok(rows.Select(x => new
        {
            x.Barcode,
            x.Name,
            x.ImageUrl,
            x.Quantity,
            x.Price
        }));
    }
}

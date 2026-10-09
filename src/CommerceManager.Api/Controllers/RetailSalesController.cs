using CommerceManager.Infrastructure.Catalog;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/retail-sales")]
public sealed class RetailSalesController(
    RetailSaleStore store,
    TrendyolAllocationPublisher trendyolAllocation) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await store.ListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRetailSaleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await store.AddAsync(
            request.CatalogProductId,
            request.Quantity,
            request.UnitPrice,
            request.CustomerName,
            request.CustomerPhone,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        await trendyolAllocation.PushAsync(request.CatalogProductId, cancellationToken);
        return Ok();
    }

    public sealed class CreateRetailSaleRequest
    {
        public Guid CatalogProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }
    }
}

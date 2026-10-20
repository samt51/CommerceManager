using CommerceManager.Application.Catalog;
using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Infrastructure.Integrations.TSoft;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/catalog-products")]
public sealed class CatalogProductsController(
    ISender sender,
    ICatalogProductStore catalogProducts,
    ITrendyolStockPublisher trendyolStock,
    TSoftStockPublisher websiteStock,
    ITrendyolInventoryReader trendyolInventory) : ControllerBase
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
                new ListCatalogProducts(page, size, search),
                cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCatalogProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCatalogProduct(
                request.Name,
                request.Quantity,
                request.SalePrice,
                request.Description,
                request.ImageUrl,
                request.TrendyolBarcode,
                request.WebsiteBarcode,
                request.TrendyolUnits < 1 ? 1 : request.TrendyolUnits,
                request.WebsiteUnits < 1 ? 1 : request.WebsiteUnits,
                (request.Allocations ?? [])
                    .Select(x => new CommerceManager.Application.Catalog.CatalogAllocationRow(
                        x.Channel,
                        x.Quantity))
                    .ToList(),
                request.Barcodes?
                    .Select(x => new CatalogBarcodeLink(x.Channel, x.Barcode, x.Units))
                    .ToList()),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { id = result.Id });
    }

    [HttpGet("{id:guid}/trendyol-live")]
    public async Task<IActionResult> TrendyolLive(Guid id, CancellationToken cancellationToken)
    {
        var listings = await catalogProducts.TrendyolListingsAsync(id, cancellationToken);
        var items = new List<object>();

        foreach (var listing in listings)
        {
            int? live = null;
            try
            {
                live = (await trendyolInventory.GetByBarcodeAsync(listing.Barcode, cancellationToken))?.Quantity;
            }
            catch (HttpRequestException)
            {
                live = null;
            }

            items.Add(new
            {
                listing.Barcode,
                listing.Units,
                listing.CardQuantity,
                listing.ListPrice,
                liveQuantity = live
            });
        }

        return Ok(new { items });
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCatalogProductRequest request,
        CancellationToken cancellationToken)
    {
        var saved = await catalogProducts.UpdateAsync(
            id,
            request.Name,
            request.Description,
            request.SalePrice,
            request.ImageUrl,
            request.Quantity,
            request.TrendyolQuantity,
            request.WebsiteQuantity,
            request.Barcodes ?? [],
            request.TrendyolListPrice,
            cancellationToken);

        if (!saved.Success)
            return BadRequest(new { message = saved.Error });

        var barcodes = saved.TrendyolBarcodes ?? [];
        string? batchId = null;
        if (barcodes.Count == 0)
        {
            await PushWebsiteAsync(request, cancellationToken);
            return Ok(new { sent = false });
        }

        var sendPrice = request.TrendyolListPrice is > 0 &&
            request.TrendyolListPrice != saved.PreviousTrendyolListPrice
            ? request.TrendyolListPrice
            : null;
        var first = true;
        foreach (var link in barcodes)
        {
            var listingQuantity = link.Units <= 1
                ? request.TrendyolQuantity
                : request.TrendyolQuantity / link.Units;

            var pushed = await trendyolStock.PushQuantityAsync(
                link.Barcode,
                listingQuantity,
                first ? sendPrice : null,
                cancellationToken);
            first = false;

            if (!pushed.Success)
                return BadRequest(new { message = pushed.Error, saved = true });

            batchId = pushed.BatchRequestId;
        }

        var websiteError = await PushWebsiteAsync(request, cancellationToken);
        if (websiteError is not null)
            return BadRequest(new { message = websiteError, saved = true });

        return Ok(new { sent = true, batchRequestId = batchId });
    }

    private async Task<string?> PushWebsiteAsync(
        UpdateCatalogProductRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var link in request.Barcodes ?? [])
        {
            if (link.Channel != 2 || string.IsNullOrWhiteSpace(link.Barcode))
                continue;

            var listingQuantity = link.Units <= 1
                ? request.WebsiteQuantity
                : request.WebsiteQuantity / link.Units;
            var pushed = await websiteStock.PushAsync(link.Barcode, listingQuantity, cancellationToken);
            if (!pushed.Success)
                return pushed.Error;
        }

        return null;
    }

    public sealed class UpdateCatalogProductRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal SalePrice { get; set; }

        public string? ImageUrl { get; set; }

        public int Quantity { get; set; }

        public int TrendyolQuantity { get; set; }

        public int WebsiteQuantity { get; set; }

        public bool SendToTrendyol { get; set; }

        public decimal? TrendyolListPrice { get; set; }

        public List<CatalogBarcodeLink>? Barcodes { get; set; }
    }

    [HttpPost("{id:guid}/trendyol-stock")]
    public async Task<IActionResult> SetTrendyolStock(
        Guid id,
        [FromBody] SetTrendyolStockRequest request,
        CancellationToken cancellationToken)
    {
        var saved = await catalogProducts.SetTrendyolStockAsync(
            id,
            request.Quantity,
            cancellationToken);

        if (!saved.Success)
            return BadRequest(new { message = saved.Error });

        if (!request.Send)
            return Ok(new { sent = false });

        var pushed = await trendyolStock.PushQuantityAsync(
            saved.Barcode!,
            request.Quantity,
            null,
            cancellationToken);

        return pushed.Success
            ? Ok(new { sent = true, batchRequestId = pushed.BatchRequestId })
            : BadRequest(new { message = pushed.Error, saved = true });
    }

    public sealed class SetTrendyolStockRequest
    {
        public int Quantity { get; set; }

        public bool Send { get; set; }
    }

    public sealed class CreateCatalogProductRequest
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal SalePrice { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? TrendyolBarcode { get; set; }

        public string? WebsiteBarcode { get; set; }

        public int TrendyolUnits { get; set; } = 1;

        public int WebsiteUnits { get; set; } = 1;

        public List<CatalogBarcodeLink>? Barcodes { get; set; }

        public List<CatalogAllocationRow> Allocations { get; set; } = [];
    }

    public sealed class CatalogAllocationRow
    {
        public int Channel { get; set; }

        public int Quantity { get; set; }
    }
}

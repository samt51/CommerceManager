using CommerceManager.Application.Catalog;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class CatalogProductReader(AppDbContext db) : ICatalogProductReader
{
    public async Task<CatalogProductPage> ListAsync(
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.CatalogProducts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(product =>
                product.Name.Contains(term) ||
                db.CatalogBarcodes.Any(barcode =>
                    barcode.CatalogProductId == product.Id &&
                    barcode.Barcode.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var totalQuantity = total == 0
            ? 0
            : await query.SumAsync(x => x.Quantity, cancellationToken);

        var products = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var ids = products.Select(x => x.Id).ToArray();

        var barcodes = await db.CatalogBarcodes
            .AsNoTracking()
            .Where(x => ids.Contains(x.CatalogProductId))
            .ToListAsync(cancellationToken);

        var allocations = await db.CatalogChannelAllocations
            .AsNoTracking()
            .Where(x => ids.Contains(x.CatalogProductId))
            .ToListAsync(cancellationToken);

        var items = products
            .Select(product =>
            {
                var productBarcodes = barcodes
                    .Where(x => x.CatalogProductId == product.Id)
                    .ToList();

                var productAllocations = allocations
                    .Where(x => x.CatalogProductId == product.Id)
                    .OrderBy(x => x.Channel)
                    .Select(x => new CatalogAllocationRow(
                        (int)x.Channel,
                        x.Quantity))
                    .ToList();

                var retailQuantity =
                    product.Quantity - productAllocations.Sum(x => x.Quantity);

                return new CatalogProductRow(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.SalePrice,
                    product.ImageUrl,
                    product.Quantity,
                    productBarcodes
                        .Where(x => x.Channel == SalesChannel.Trendyol)
                        .OrderBy(x => x.Units)
                        .Select(x => x.Barcode)
                        .FirstOrDefault(),
                    productBarcodes
                        .Where(x => x.Channel == SalesChannel.Website)
                        .OrderBy(x => x.Units)
                        .Select(x => x.Barcode)
                        .FirstOrDefault(),
                    retailQuantity,
                    productAllocations,
                    productBarcodes
                        .OrderBy(x => x.Channel)
                        .ThenBy(x => x.Units)
                        .Select(x => new CatalogBarcodeLink(
                            (int)x.Channel,
                            x.Barcode,
                            x.Units < 1 ? 1 : x.Units))
                        .ToList(),
                    product.CreatedAt);
            })
            .ToList();

        return new CatalogProductPage(
            page,
            size,
            total,
            totalQuantity,
            items);
    }
}

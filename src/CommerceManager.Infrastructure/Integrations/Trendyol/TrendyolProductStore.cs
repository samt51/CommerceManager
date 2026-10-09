using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Catalog;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Products;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolProductStore(
    AppDbContext db,
    ITrendyolProductReader productReader,
    MarketplaceIntegrationStatusStore statusStore)
    : ITrendyolProductStore
{
    public async Task<TrendyolProductSyncResult> SyncAsync(
        CancellationToken cancellationToken)
    {
        const int size = 100;

        var received = 0;
        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var catalogInserted = 0;

        var seenBarcodes =
            new HashSet<string>(StringComparer.Ordinal);

        for (var page = 0; ; page++)
        {
            var result =
                await productReader.ReadPageAsync(
                    page,
                    size,
                    cancellationToken);

            foreach (var item in result.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Barcode))
                    continue;

                // Canlı katalog sayfalama sırasında değişirse
                // aynı barkodu iki kez işlemeyelim.
                if (!seenBarcodes.Add(item.Barcode))
                    continue;

                received++;

                var product =
                    await db.Products.FirstOrDefaultAsync(
                        x =>
                            x.Channel == SalesChannel.Trendyol &&
                            x.Barcode == item.Barcode,
                        cancellationToken);

                var name =
                    item.Title?.Trim() ?? string.Empty;

                if (product is null)
                {
                    product = Product.Create(
                        SalesChannel.Trendyol,
                        item.Barcode,
                        name,
                        item.StockCode,
                        item.ExternalProductId,
                        item.ProductMainId,
                        item.ImageUrl);

                    db.Products.Add(product);
                    inserted++;
                }
                else if (IsSame(product, item, name))
                {
                    unchanged++;
                }
                else
                {
                    product.Update(
                        name,
                        item.StockCode,
                        item.ExternalProductId,
                        item.ProductMainId,
                        item.ImageUrl);

                    updated++;
                }

                if (await AddCatalogProductAsync(
                        item.Barcode,
                        name,
                        item.Quantity,
                        cancellationToken))
                {
                    catalogInserted++;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            if (result.TotalPages <= 0 ||
                page + 1 >= result.TotalPages)
            {
                break;
            }
        }

        await statusStore.MarkProductSyncSucceededAsync(
            SalesChannel.Trendyol,
            cancellationToken);

        return new TrendyolProductSyncResult(
            received,
            inserted,
            updated,
            unchanged,
            catalogInserted);
    }

    private async Task<bool> AddCatalogProductAsync(
        string barcode,
        string name,
        int quantity,
        CancellationToken cancellationToken)
    {
        var normalized = barcode.Trim();

        var exists = await db.CatalogBarcodes.AnyAsync(
            x =>
                x.Channel == SalesChannel.Trendyol &&
                x.Barcode == normalized,
            cancellationToken);

        if (exists)
            return false;

        var catalogName = string.IsNullOrWhiteSpace(name)
            ? normalized
            : name.Trim();

        if (catalogName.Length > 300)
            catalogName = catalogName[..300];

        var onHand = Math.Max(quantity, 0);
        var catalogProduct = CatalogProduct.Create(catalogName, onHand);

        db.CatalogProducts.Add(catalogProduct);
        db.CatalogBarcodes.Add(
            CatalogBarcode.Create(
                catalogProduct.Id,
                SalesChannel.Trendyol,
                normalized));

        if (onHand > 0)
        {
            db.CatalogChannelAllocations.Add(
                CatalogChannelAllocation.Create(
                    catalogProduct.Id,
                    SalesChannel.Trendyol,
                    onHand));
        }

        return true;
    }

    private static bool IsSame(
        Product product,
        TrendyolProductSnapshot item,
        string name)
        =>
            string.Equals(
                product.Name,
                name,
                StringComparison.Ordinal) &&
            string.Equals(
                product.StockCode,
                Normalize(item.StockCode),
                StringComparison.Ordinal) &&
            string.Equals(
                product.ExternalProductId,
                Normalize(item.ExternalProductId),
                StringComparison.Ordinal) &&
            string.Equals(
                product.ProductMainId,
                Normalize(item.ProductMainId),
                StringComparison.Ordinal) &&
            string.Equals(
                product.ImageUrl,
                Normalize(item.ImageUrl),
                StringComparison.Ordinal);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

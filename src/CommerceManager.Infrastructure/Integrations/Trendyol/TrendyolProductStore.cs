using CommerceManager.Application.Integrations.Trendyol;
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

                    continue;
                }

                if (IsSame(product, item, name))
                {
                    unchanged++;
                    continue;
                }

                product.Update(
                    name,
                    item.StockCode,
                    item.ExternalProductId,
                    item.ProductMainId,
                    item.ImageUrl);

                updated++;
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
            unchanged);
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

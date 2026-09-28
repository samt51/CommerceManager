using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolProductImageStore(
    AppDbContext db,
    ITrendyolProductReader productReader)
    : ITrendyolProductImageStore
{
    public async Task<TrendyolProductImageBackfillResult> BackfillAsync(
        CancellationToken cancellationToken)
    {
        var barcodes = await (
            from item in db.OrderItems
            join order in db.Orders
                on item.OrderId equals order.Id
            where order.Channel == SalesChannel.Trendyol
                  && item.Barcode != null
                  && item.Barcode != ""
                  && item.ImageUrl == null
            select item.Barcode!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var found = 0;
        var notFound = 0;
        var updated = 0;

        foreach (var barcode in barcodes)
        {
            var product =
                await productReader.GetByBarcodeAsync(
                    barcode,
                    cancellationToken);

            if (product is null ||
                string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                notFound++;
                continue;
            }

            found++;

            var items = await (
                from item in db.OrderItems
                join order in db.Orders
                    on item.OrderId equals order.Id
                where order.Channel == SalesChannel.Trendyol
                      && item.Barcode == barcode
                      && item.ImageUrl == null
                select item)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.SetImageUrl(product.ImageUrl);
                updated++;
            }

            await db.SaveChangesAsync(cancellationToken);

            // Trendyol'a arka arkaya çok hızlı istek göndermeyelim.
            await Task.Delay(
                TimeSpan.FromMilliseconds(150),
                cancellationToken);
        }

        return new TrendyolProductImageBackfillResult(
            barcodes.Count,
            found,
            notFound,
            updated);
    }
}

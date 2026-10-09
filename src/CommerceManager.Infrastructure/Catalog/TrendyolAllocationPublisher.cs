using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommerceManager.Infrastructure.Catalog;

public sealed class TrendyolAllocationPublisher(
    AppDbContext db,
    ITrendyolStockPublisher publisher,
    ILogger<TrendyolAllocationPublisher> logger)
{
    public async Task PushAsync(Guid productId, CancellationToken cancellationToken)
    {
        var allocation = await db.CatalogChannelAllocations.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol,
                cancellationToken);

        if (allocation is null)
            return;

        var barcodes = await db.CatalogBarcodes.AsNoTracking()
            .Where(x => x.CatalogProductId == productId && x.Channel == SalesChannel.Trendyol)
            .ToListAsync(cancellationToken);

        foreach (var barcode in barcodes)
        {
            var quantity = allocation.Quantity / Math.Max(barcode.Units, 1);
            var result = await publisher.PushQuantityAsync(
                barcode.Barcode,
                quantity,
                null,
                cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning(
                    "Perakende sonrası Trendyol stoğu gönderilemedi. Barkod {Barcode}. {Error}",
                    barcode.Barcode,
                    result.Error);
            }
        }
    }
}

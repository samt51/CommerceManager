using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Integrations;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolInventoryStore(
    AppDbContext db,
    ITrendyolInventoryReader reader,
    MarketplaceIntegrationStatusStore statusStore)
    : ITrendyolInventoryStore
{
    public async Task<TrendyolInventorySyncResult> SyncAsync(
        CancellationToken cancellationToken)
    {
        const int size = 100;

        var received = 0;
        var inserted = 0;
        var skipped = 0;

        var page = 0;

        while (true)
        {
            var result =
                await reader.ReadPageAsync(
                    page,
                    size,
                    cancellationToken);

            foreach (var item in result.Items)
            {
                received++;

                var stock =
                    await db.ChannelStocks
                        .SingleOrDefaultAsync(
                            x =>
                                x.Channel == SalesChannel.Trendyol &&
                                x.Barcode == item.Barcode,
                            cancellationToken);

                // ChannelStock mevcutsa internal miktara dokunma.
                // Bundan sonra satış/iade hareketleri internal stoğu yönetir.
                if (stock is not null)
                {
                    skipped++;
                    continue;
                }

                db.ChannelStocks.Add(
                    ChannelStock.Create(
                        SalesChannel.Trendyol,
                        item.Barcode,
                        item.Quantity));

                inserted++;
            }

            await db.SaveChangesAsync(cancellationToken);

            if (page + 1 >= result.TotalPages)
                break;

            page++;
        }

        await statusStore.MarkInventorySyncSucceededAsync(
            SalesChannel.Trendyol,
            cancellationToken);

        return new TrendyolInventorySyncResult(
            received,
            inserted,
            skipped);
    }
}

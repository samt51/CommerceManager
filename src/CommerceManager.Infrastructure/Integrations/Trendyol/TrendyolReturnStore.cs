using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using CommerceManager.Infrastructure.Catalog;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolReturnStore(
    AppDbContext db,
    ITrendyolInventoryReader inventoryReader,
    CatalogStockApplier catalogStock,
    ILogger<TrendyolReturnStore> logger)
    : ITrendyolReturnStore
{
    public async Task<TrendyolReturnStoreResult> UpsertAsync(
        TrendyolReturnSnapshot claim,
        TrendyolReturnItemSnapshot item,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(
                x => x.Channel == SalesChannel.Trendyol &&
                     x.OrderNumber == claim.OrderNumber,
                cancellationToken);

        if (order is null)
        {
            return new TrendyolReturnStoreResult(
                false, false, true, false);
        }

        var orderItem = order.Items.SingleOrDefault(
            x => x.ExternalLineId == item.OrderLineId);

        if (orderItem is null)
        {
            return new TrendyolReturnStoreResult(
                false, false, false, true);
        }

        var status = MapStatus(item.Status);

        var orderReturn =
            await db.OrderReturns.SingleOrDefaultAsync(
                x => x.ExternalReturnId == item.ClaimItemId,
                cancellationToken);

        var inserted = orderReturn is null;

        if (orderReturn is null)
        {
            orderReturn = OrderReturn.Create(
                order.Id,
                orderItem.Id,
                item.ClaimItemId,
                1,
                status,
                claim.LastModifiedAt);

            db.OrderReturns.Add(orderReturn);
        }
        else
        {
            orderReturn.Update(
                1,
                status,
                claim.LastModifiedAt);
        }

        var appliedNow = false;

        if (status == ReturnStatus.Accepted &&
            !orderReturn.StockApplied)
        {
            if (string.IsNullOrWhiteSpace(orderItem.Barcode))
            {
                throw new InvalidOperationException(
                    $"Order item {orderItem.Id} has no barcode.");
            }

            var barcode = orderItem.Barcode;

            var stock = await db.ChannelStocks
                .SingleOrDefaultAsync(
                    x => x.Channel == SalesChannel.Trendyol &&
                         x.Barcode == barcode,
                    cancellationToken);

            if (stock is null)
            {
                var inventory =
                    await inventoryReader.GetByBarcodeAsync(
                        barcode,
                        cancellationToken);

                stock = ChannelStock.Create(
                    SalesChannel.Trendyol,
                    barcode,
                    inventory?.Quantity ?? 0);

                db.ChannelStocks.Add(stock);
            }

            stock.Increase(1);

            var movementExists =
                await db.StockMovements.AnyAsync(
                    x => x.Channel == SalesChannel.Trendyol &&
                         x.ReferenceType == "TrendyolReturn" &&
                         x.ReferenceId == item.ClaimItemId &&
                         x.Barcode == barcode &&
                         x.Type == StockMovementType.Return,
                    cancellationToken);

            if (!movementExists)
            {
                db.StockMovements.Add(
                    StockMovement.Create(
                        SalesChannel.Trendyol,
                        barcode,
                        StockMovementType.Return,
                        1,
                        "TrendyolReturn",
                        item.ClaimItemId));
            }

            var catalogApplied = await catalogStock.TryApplyAsync(
                SalesChannel.Trendyol,
                barcode,
                1,
                StockMovementType.Return,
                "TrendyolReturn",
                item.ClaimItemId,
                cancellationToken);

            if (!catalogApplied)
            {
                logger.LogWarning(
                    "Genel stok ürünü bulunamadı. Barkod {Barcode} eşleşmediği için iade {ClaimItemId} genel stoka eklenmedi.",
                    barcode,
                    item.ClaimItemId);
            }

            orderReturn.MarkStockApplied(
                claim.LastModifiedAt);

            appliedNow = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new TrendyolReturnStoreResult(
            inserted,
            appliedNow,
            false,
            false);
    }

    private static ReturnStatus MapStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "accepted" => ReturnStatus.Accepted,
            "rejected" => ReturnStatus.Rejected,
            "cancelled" => ReturnStatus.Cancelled,

            "created" => ReturnStatus.Pending,
            "waitinginaction" => ReturnStatus.Pending,
            "unresolved" => ReturnStatus.Pending,
            "inanalysis" => ReturnStatus.Pending,

            _ => ReturnStatus.Pending
        };
}

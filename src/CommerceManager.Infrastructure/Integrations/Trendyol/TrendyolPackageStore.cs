using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CommerceManager.Domain.Inventory;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolPackageStore(
    AppDbContext db,
    ITrendyolProductReader productReader,
    ILogger<TrendyolPackageStore> logger)
    : ITrendyolPackageStore
{
    public async Task<bool> UpsertAsync(
        TrendyolPackageSnapshot package,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(
                x => x.Channel == SalesChannel.Trendyol &&
                     x.ExternalOrderId == package.PackageId,
                cancellationToken);

        var isNew = order is null;

        if (isNew)
        {
            order = Order.Create(
                SalesChannel.Trendyol,
                package.PackageId,
                package.OrderedAt,
                package.TotalAmount);

            db.Orders.Add(order);
        }

        order!.UpdateTrendyolPackage(
            package.OrderNumber,
            package.Status,
            package.LastModifiedAt,
            package.TotalAmount);

        // Aynı paket içinde aynı barkod için tekrar tekrar
        // DB/API sorgusu yapmamak için local cache.
        var imageCache =
            new Dictionary<string, string?>(
                StringComparer.Ordinal);

        foreach (var line in package.Lines)
        {
            var existing =
                order.Items.SingleOrDefault(
                    x => x.ExternalLineId == line.LineId);

            OrderItem item;

            if (existing is null)
            {
                item = OrderItem.Create(
                    line.LineId,
                    line.Barcode,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice);

                order.Items.Add(item);
            }
            else
            {
                existing.Update(
                    line.Barcode,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice);

                item = existing;
            }
            // Yeni sipariş satırı için Trendyol kanal stoğunu düş.
// StockMovement unique index'i sayesinde aynı sipariş satırı
// tekrar senkronize edilirse stok ikinci kez düşmez.
            if (isNew &&
                !string.IsNullOrWhiteSpace(line.Barcode))
            {
                var referenceType = "TrendyolOrderItem";
                var referenceId = $"{package.PackageId}:{line.LineId}";

                var movementExists =
                    await db.StockMovements.AnyAsync(
                        x =>
                            x.Channel == SalesChannel.Trendyol &&
                            x.Barcode == line.Barcode &&
                            x.Type == StockMovementType.Sale &&
                            x.ReferenceType == referenceType &&
                            x.ReferenceId == referenceId,
                        cancellationToken);

                if (!movementExists)
                {
                    var stock =
                        await db.ChannelStocks.SingleOrDefaultAsync(
                            x =>
                                x.Channel == SalesChannel.Trendyol &&
                                x.Barcode == line.Barcode,
                            cancellationToken);

                    if (stock is null)
                    {
                        logger.LogWarning(
                            "Trendyol stock record not found for barcode {Barcode}. Order {OrderId} stock movement was not created.",
                            line.Barcode,
                            package.PackageId);
                    }
                    else
                    {
                        stock.Decrease(line.Quantity);

                        db.StockMovements.Add(
                            StockMovement.Create(
                                SalesChannel.Trendyol,
                                line.Barcode,
                                StockMovementType.Sale,
                                line.Quantity,
                                referenceType,
                                referenceId));
                    }
                }
            }

            // Görsel zaten varsa hiçbir işlem yapma.
            if (!string.IsNullOrWhiteSpace(item.ImageUrl) ||
                string.IsNullOrWhiteSpace(line.Barcode))
            {
                continue;
            }

            var imageUrl = await ResolveImageUrlAsync(
                line.Barcode,
                imageCache,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                item.SetImageUrl(imageUrl);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return isNew;
    }

    private async Task<string?> ResolveImageUrlAsync(
        string barcode,
        Dictionary<string, string?> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(barcode, out var cached))
        {
            return cached;
        }

        // Önce kendi DB'mizde aynı barkod için daha önce
        // kaydedilmiş bir görsel var mı kontrol et.
        var existingImageUrl = await (
            from item in db.OrderItems.AsNoTracking()
            join order in db.Orders.AsNoTracking()
                on item.OrderId equals order.Id
            where order.Channel == SalesChannel.Trendyol
                  && item.Barcode == barcode
                  && item.ImageUrl != null
                  && item.ImageUrl != ""
            select item.ImageUrl)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(existingImageUrl))
        {
            cache[barcode] = existingImageUrl;
            return existingImageUrl;
        }

        try
        {
            var product =
                await productReader.GetByBarcodeAsync(
                    barcode,
                    cancellationToken);

            var imageUrl = product?.ImageUrl;

            cache[barcode] = imageUrl;

            return imageUrl;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Ürün görselindeki problem sipariş senkronizasyonunu
            // durdurmamalı.
            logger.LogWarning(
                exception,
                "Trendyol product image could not be resolved for barcode {Barcode}.",
                barcode);

            cache[barcode] = null;

            return null;
        }
    }
}

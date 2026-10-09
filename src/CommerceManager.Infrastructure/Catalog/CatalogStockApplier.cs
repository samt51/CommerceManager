using CommerceManager.Domain.Catalog;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Catalog;

public sealed class CatalogStockApplier(AppDbContext db)
{
    public async Task<bool> TryApplyAsync(
        SalesChannel channel,
        string barcode,
        int quantity,
        StockMovementType type,
        string referenceType,
        string referenceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode) || quantity <= 0)
            return false;

        var normalized = barcode.Trim();

        var link = await db.CatalogBarcodes.SingleOrDefaultAsync(
            x => x.Channel == channel &&
                 x.Barcode == normalized,
            cancellationToken);

        if (link is null)
            return false;

        var alreadyApplied = await db.CatalogStockMovements.AnyAsync(
            x => x.Channel == channel &&
                 x.Barcode == normalized &&
                 x.Type == type &&
                 x.ReferenceType == referenceType &&
                 x.ReferenceId == referenceId,
            cancellationToken);

        if (alreadyApplied)
            return true;

        var product = await db.CatalogProducts.SingleAsync(
            x => x.Id == link.CatalogProductId,
            cancellationToken);

        var pieces = quantity * Math.Max(link.Units, 1);

        if (type == StockMovementType.Return)
            product.Increase(pieces);
        else
            product.Decrease(pieces);

        var allocation = await db.CatalogChannelAllocations
            .SingleOrDefaultAsync(
                x => x.CatalogProductId == product.Id &&
                     x.Channel == channel,
                cancellationToken);

        if (allocation is not null)
        {
            if (type == StockMovementType.Return)
                allocation.Increase(pieces);
            else
                allocation.Decrease(pieces);
        }

        db.CatalogStockMovements.Add(
            CatalogStockMovement.Create(
                product.Id,
                channel,
                normalized,
                type,
                pieces,
                referenceType,
                referenceId));

        return true;
    }
}

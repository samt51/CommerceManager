using CommerceManager.Application.Cash;
using CommerceManager.Domain.Cash;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class RetailSaleStore(AppDbContext db)
{
    public async Task<LedgerCommandResult> AddAsync(
        Guid catalogProductId,
        int quantity,
        decimal unitPrice,
        string? customerName,
        string? customerPhone,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
            return new LedgerCommandResult(false, "Adet 0'dan büyük olmalı.");

        if (unitPrice <= 0)
            return new LedgerCommandResult(false, "Birim fiyat 0'dan büyük olmalı.");

        var product = await db.CatalogProducts
            .SingleOrDefaultAsync(x => x.Id == catalogProductId, cancellationToken);

        if (product is null)
            return new LedgerCommandResult(false, "Ürün bulunamadı.");

        var allocated = await db.CatalogChannelAllocations
            .Where(x => x.CatalogProductId == catalogProductId)
            .SumAsync(x => (int?)x.Quantity, cancellationToken) ?? 0;

        var retailQuantity = product.Quantity - allocated;

        if (retailQuantity < quantity)
            return new LedgerCommandResult(false, "Perakende için yeterli stok yok.");

        var sale = RetailSale.Create(
            catalogProductId,
            quantity,
            unitPrice,
            customerName,
            customerPhone);
        product.Decrease(quantity);

        db.RetailSales.Add(sale);
        db.CashMovements.Add(
            CashMovement.Create(
                CashDirection.In,
                sale.TotalAmount,
                $"Perakende satış: {product.Name} x {quantity}",
                sale.OccurredAt));

        await db.SaveChangesAsync(cancellationToken);

        return new LedgerCommandResult(true, null);
    }

    public async Task<IReadOnlyList<RetailSaleListItem>> ListAsync(
        CancellationToken cancellationToken)
    {
        return await (
            from sale in db.RetailSales.AsNoTracking()
            join product in db.CatalogProducts.AsNoTracking()
                on sale.CatalogProductId equals product.Id
            orderby sale.OccurredAt descending
            select new RetailSaleListItem(
                sale.Id,
                product.Name,
                sale.CustomerName,
                sale.CustomerPhone,
                sale.Quantity,
                sale.UnitPrice,
                sale.TotalAmount,
                sale.OccurredAt))
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}

public sealed record RetailSaleListItem(
    Guid Id,
    string ProductName,
    string? CustomerName,
    string? CustomerPhone,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    DateTimeOffset OccurredAt);

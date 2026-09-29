using CommerceManager.Application.Orders;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class OrderReader(AppDbContext db) : IOrderReader
{
    public async Task<OrderPage> ListTrendyolAsync(int page, int size,
        string? status, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.Channel == SalesChannel.Trendyol);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.OrderedAt)
            .ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size)
            .Select(x => new OrderRow(
                x.Id,
                x.ExternalOrderId,
                x.OrderNumber,
                x.Status,
                x.OrderedAt,
                x.TotalAmount,
                x.Items.Count,
                x.Items
                    .OrderBy(i => i.ExternalLineId)
                    .Select(i => i.ProductName)
                    .FirstOrDefault(),
                x.Items
                    .OrderBy(i => i.ExternalLineId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new OrderPage(page, size, total, rows);
    }

    public Task<OrderDetails?> GetTrendyolAsync(Guid id,
        CancellationToken cancellationToken) => db.Orders.AsNoTracking()
        .Where(x => x.Id == id && x.Channel == SalesChannel.Trendyol)
        .Select(x => new OrderDetails(x.Id, x.ExternalOrderId,
            x.OrderNumber, x.Status, x.OrderedAt, x.ExternalLastModifiedAt,
            x.TotalAmount, x.Items.OrderBy(i => i.ExternalLineId)
                .Select(i => new OrderLineRow(i.ExternalLineId, i.Barcode,
                    i.ProductName, i.ImageUrl, i.Quantity, i.UnitPrice)).ToList()))
        .SingleOrDefaultAsync(cancellationToken);
}

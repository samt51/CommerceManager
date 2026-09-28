using CommerceManager.Application.Returns;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class ReturnReader(AppDbContext db) : IReturnReader
{
    public async Task<ReturnPage> ListAsync(
        int page,
        int size,
        ReturnStatus? status,
        SalesChannel? channel,
        CancellationToken cancellationToken)
    {
        var query =
            from orderReturn in db.OrderReturns.AsNoTracking()
            join order in db.Orders.AsNoTracking()
                on orderReturn.OrderId equals order.Id
            join item in db.OrderItems.AsNoTracking()
                on orderReturn.OrderItemId equals item.Id
            select new
            {
                Return = orderReturn,
                Order = order,
                Item = item
            };

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Return.Status == status.Value);
        }

        if (channel.HasValue)
        {
            query = query.Where(
                x => x.Order.Channel == channel.Value);
        }

        var total =
            await query.CountAsync(cancellationToken);

        var rows =
            await query
                .OrderByDescending(
                    x => x.Return.ExternalLastModifiedAt)
                .ThenByDescending(
                    x => x.Return.Id)
                .Skip(page * size)
                .Take(size)
                .Select(x => new ReturnRow(
                    x.Return.Id,
                    x.Return.ExternalReturnId,
                    x.Order.Id,
                    x.Order.OrderNumber,
                    x.Order.Channel,
                    x.Item.Barcode,
                    x.Item.ProductName,
                    x.Item.ImageUrl,
                    x.Return.Quantity,
                    x.Return.Status,
                    x.Return.StockApplied,
                    x.Return.AcceptedAt,
                    x.Return.ExternalLastModifiedAt,
                    x.Return.CreatedAt))
                .ToListAsync(cancellationToken);

        return new ReturnPage(
            page,
            size,
            total,
            rows);
    }

    public Task<ReturnDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return (
            from orderReturn in db.OrderReturns.AsNoTracking()
            join order in db.Orders.AsNoTracking()
                on orderReturn.OrderId equals order.Id
            join item in db.OrderItems.AsNoTracking()
                on orderReturn.OrderItemId equals item.Id
            where orderReturn.Id == id
            select new ReturnDetails(
                orderReturn.Id,
                orderReturn.ExternalReturnId,
                order.Id,
                item.Id,
                order.OrderNumber,
                order.ExternalOrderId,
                order.Channel,
                item.Barcode,
                item.ProductName,
                item.ImageUrl,
                orderReturn.Quantity,
                item.UnitPrice,
                orderReturn.Status,
                orderReturn.StockApplied,
                orderReturn.AcceptedAt,
                orderReturn.ExternalLastModifiedAt,
                orderReturn.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

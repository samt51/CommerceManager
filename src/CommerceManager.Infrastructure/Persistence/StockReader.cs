using CommerceManager.Application.Inventory;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class StockReader(
    AppDbContext db)
    : IStockReader
{
    public async Task<StockPage> ListTrendyolAsync(
        int page,
        int size,
        string? search,
        string? stockStatus,
        CancellationToken cancellationToken)
    {
        var baseQuery =
            from stock in db.ChannelStocks.AsNoTracking()
            join product in db.Products.AsNoTracking()
                on new
                {
                    stock.Channel,
                    stock.Barcode
                }
                equals new
                {
                    product.Channel,
                    product.Barcode
                }
            where stock.Channel == SalesChannel.Trendyol
            select new
            {
                ProductId = product.Id,
                product.Barcode,
                product.StockCode,
                ProductName = product.Name,
                product.ImageUrl,
                stock.Quantity,
                StockUpdatedAt = stock.UpdatedAt
            };

        var totalQuantity =
            await baseQuery.SumAsync(
                x => x.Quantity,
                cancellationToken);

        var inStockCount =
            await baseQuery.CountAsync(
                x => x.Quantity > 5,
                cancellationToken);

        var lowStockCount =
            await baseQuery.CountAsync(
                x => x.Quantity > 0 &&
                     x.Quantity <= 5,
                cancellationToken);

        var outOfStockCount =
            await baseQuery.CountAsync(
                x => x.Quantity == 0,
                cancellationToken);

        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(
                x =>
                    x.ProductName.Contains(term) ||
                    x.Barcode.Contains(term) ||
                    (x.StockCode != null &&
                     x.StockCode.Contains(term)));
        }

        switch (stockStatus?.Trim().ToLowerInvariant())
        {
            case "instock":
                query = query.Where(x => x.Quantity > 5);
                break;

            case "lowstock":
                query = query.Where(
                    x => x.Quantity > 0 &&
                         x.Quantity <= 5);
                break;

            case "outofstock":
                query = query.Where(x => x.Quantity == 0);
                break;
        }

        var total =
            await query.CountAsync(cancellationToken);

        var rows =
            await query
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.Barcode)
                .Skip(page * size)
                .Take(size)
                .Select(x => new StockRow(
                    x.ProductId,
                    x.Barcode,
                    x.StockCode,
                    x.ProductName,
                    x.ImageUrl,
                    x.Quantity,
                    x.StockUpdatedAt))
                .ToListAsync(cancellationToken);

        return new StockPage(
            page,
            size,
            total,
            totalQuantity,
            inStockCount,
            lowStockCount,
            outOfStockCount,
            rows);
    }
}

using CommerceManager.Application.Products;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class ProductReader(
    AppDbContext db)
    : IProductReader
{
    public async Task<ProductPage> ListTrendyolAsync(
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken)
    {
        var products = db.Products
            .AsNoTracking()
            .Where(x => x.Channel == SalesChannel.Trendyol);

        var totalCatalogCount =
            await products.CountAsync(cancellationToken);

        var withStockCount =
            await (
                from product in products
                join stock in db.ChannelStocks.AsNoTracking()
                    on new
                    {
                        product.Channel,
                        product.Barcode
                    }
                    equals new
                    {
                        stock.Channel,
                        stock.Barcode
                    }
                select product.Id
            ).CountAsync(cancellationToken);

        var withoutStockCount =
            totalCatalogCount - withStockCount;

        var query =
            from product in products
            join stock in db.ChannelStocks.AsNoTracking()
                on new
                {
                    product.Channel,
                    product.Barcode
                }
                equals new
                {
                    stock.Channel,
                    stock.Barcode
                }
                into stockJoin
            from stock in stockJoin.DefaultIfEmpty()
            select new
            {
                product.Id,
                product.Barcode,
                product.StockCode,
                product.ExternalProductId,
                product.ProductMainId,
                product.Name,
                product.ImageUrl,
                Quantity = stock == null
                    ? (int?)null
                    : stock.Quantity,
                product.UpdatedAt
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.Barcode.Contains(term) ||
                (x.StockCode != null &&
                 x.StockCode.Contains(term)) ||
                (x.ProductMainId != null &&
                 x.ProductMainId.Contains(term)));
        }

        var total =
            await query.CountAsync(cancellationToken);

        var rows =
            await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Barcode)
                .Skip(page * size)
                .Take(size)
                .Select(x => new ProductRow(
                    x.Id,
                    x.Barcode,
                    x.StockCode,
                    x.ExternalProductId,
                    x.ProductMainId,
                    x.Name,
                    x.ImageUrl,
                    x.Quantity,
                    x.UpdatedAt))
                .ToListAsync(cancellationToken);

        return new ProductPage(
            page,
            size,
            total,
            withStockCount,
            withoutStockCount,
            rows);
    }

    public async Task<ProductDetails?> GetTrendyolAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query =
            from product in db.Products.AsNoTracking()
            where product.Id == id &&
                  product.Channel == SalesChannel.Trendyol

            join stock in db.ChannelStocks.AsNoTracking()
                on new
                {
                    product.Channel,
                    product.Barcode
                }
                equals new
                {
                    stock.Channel,
                    stock.Barcode
                }
                into stockJoin

            from stock in stockJoin.DefaultIfEmpty()

            select new ProductDetails(
                product.Id,
                product.Barcode,
                product.StockCode,
                product.ExternalProductId,
                product.ProductMainId,
                product.Name,
                product.ImageUrl,
                stock == null
                    ? (int?)null
                    : stock.Quantity,
                product.UpdatedAt,
                stock == null
                    ? (DateTimeOffset?)null
                    : stock.UpdatedAt);

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }

}

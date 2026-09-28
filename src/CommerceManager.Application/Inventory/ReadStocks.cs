using MediatR;

namespace CommerceManager.Application.Inventory;

public sealed record StockRow(
    Guid ProductId,
    string Barcode,
    string? StockCode,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    DateTimeOffset StockUpdatedAt);

public sealed record StockPage(
    int Page,
    int Size,
    int Total,
    int TotalQuantity,
    int InStockCount,
    int LowStockCount,
    int OutOfStockCount,
    IReadOnlyList<StockRow> Items);

public interface IStockReader
{
    Task<StockPage> ListTrendyolAsync(
        int page,
        int size,
        string? search,
        string? stockStatus,
        CancellationToken cancellationToken);
}

public sealed record ListTrendyolStocks(
    int Page = 0,
    int Size = 25,
    string? Search = null,
    string? StockStatus = null)
    : IRequest<StockPage>;

public sealed class ListTrendyolStocksHandler(
    IStockReader reader)
    : IRequestHandler<ListTrendyolStocks, StockPage>
{
    public Task<StockPage> Handle(
        ListTrendyolStocks request,
        CancellationToken cancellationToken)
        => reader.ListTrendyolAsync(
            request.Page,
            request.Size,
            request.Search,
            request.StockStatus,
            cancellationToken);
}

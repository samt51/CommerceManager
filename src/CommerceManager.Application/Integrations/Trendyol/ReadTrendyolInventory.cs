namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolInventorySnapshot(
    string Barcode,
    int Quantity,
    string? StockCode,
    decimal? SalePrice = null,
    decimal? ListPrice = null);

public sealed record TrendyolInventoryPage(
    int Page,
    int Size,
    int TotalElements,
    int TotalPages,
    string? NextPageToken,
    IReadOnlyList<TrendyolInventorySnapshot> Items);

public sealed record TrendyolStockPushResult(
    bool Success,
    string? Error,
    string? BatchRequestId);

public interface ITrendyolStockPublisher
{
    Task<TrendyolStockPushResult> PushQuantityAsync(
        string barcode,
        int quantity,
        decimal? salePrice,
        CancellationToken cancellationToken);
}

public sealed record TrendyolSettlementSummary(
    bool Available,
    decimal Commission,
    decimal Discount,
    decimal SellerRevenue);

public interface ITrendyolSettlementReader
{
    Task<TrendyolSettlementSummary> ReadRangeAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
}

public interface ITrendyolInventoryReader
{
    Task<TrendyolInventorySnapshot?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken);

    Task<TrendyolInventoryPage> ReadPageAsync(
        int page,
        int size,
        CancellationToken cancellationToken);
}

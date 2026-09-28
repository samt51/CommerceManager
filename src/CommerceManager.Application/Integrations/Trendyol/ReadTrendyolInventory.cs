namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolInventorySnapshot(
    string Barcode,
    int Quantity,
    string? StockCode);

public sealed record TrendyolInventoryPage(
    int Page,
    int Size,
    int TotalElements,
    int TotalPages,
    string? NextPageToken,
    IReadOnlyList<TrendyolInventorySnapshot> Items);

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

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolProductSnapshot(
    string Barcode,
    string? Title,
    string? ImageUrl,
    string? StockCode = null,
    string? ExternalProductId = null,
    string? ProductMainId = null,
    int Quantity = 0);

public sealed record TrendyolProductPage(
    int Page,
    int Size,
    int TotalElements,
    int TotalPages,
    string? NextPageToken,
    IReadOnlyList<TrendyolProductSnapshot> Items);

public interface ITrendyolProductReader
{
    Task<TrendyolProductSnapshot?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken);

    Task<TrendyolProductPage> ReadPageAsync(
        int page,
        int size,
        CancellationToken cancellationToken);
}

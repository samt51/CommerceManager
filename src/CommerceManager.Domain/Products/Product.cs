using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Products;

public sealed class Product
{
    private Product() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public SalesChannel Channel { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public string? StockCode { get; private set; }

    public string? ExternalProductId { get; private set; }

    public string? ProductMainId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? ImageUrl { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } =
        DateTimeOffset.UtcNow;

    public static Product Create(
        SalesChannel channel,
        string barcode,
        string name,
        string? stockCode,
        string? externalProductId,
        string? productMainId,
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.");

        return new Product
        {
            Channel = channel,
            Barcode = barcode.Trim(),
            Name = name?.Trim() ?? string.Empty,
            StockCode = Normalize(stockCode),
            ExternalProductId = Normalize(externalProductId),
            ProductMainId = Normalize(productMainId),
            ImageUrl = Normalize(imageUrl)
        };
    }

    public void Update(
        string name,
        string? stockCode,
        string? externalProductId,
        string? productMainId,
        string? imageUrl)
    {
        Name = name?.Trim() ?? string.Empty;
        StockCode = Normalize(stockCode);
        ExternalProductId = Normalize(externalProductId);
        ProductMainId = Normalize(productMainId);
        ImageUrl = Normalize(imageUrl);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

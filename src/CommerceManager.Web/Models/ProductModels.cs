namespace CommerceManager.Web.Models;

public sealed class ProductRow
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string? StockCode { get; set; }

    public string? ExternalProductId { get; set; }

    public string? ProductMainId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int? Quantity { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string StockText =>
        Quantity.HasValue
            ? Quantity.Value.ToString()
            : "—";

    public string StockStatusText =>
        !Quantity.HasValue
            ? "Stok Kaydı Yok"
            : Quantity.Value == 0
                ? "Tükendi"
                : Quantity.Value <= 5
                    ? "Düşük Stok"
                    : "Stokta";

    public string StockStatusCss =>
        !Quantity.HasValue
            ? "neutral"
            : Quantity.Value == 0
                ? "cancelled"
                : Quantity.Value <= 5
                    ? "pending"
                    : "accepted";
}

public sealed class ProductPage
{
    public int Page { get; set; }

    public int Size { get; set; } = 25;

    public int Total { get; set; }

    public int WithStockCount { get; set; }

    public int WithoutStockCount { get; set; }

    public List<ProductRow> Items { get; set; } = [];
}

public sealed class ProductDetails
{
    public Guid Id { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string? StockCode { get; set; }

    public string? ExternalProductId { get; set; }

    public string? ProductMainId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int? Quantity { get; set; }

    public DateTimeOffset ProductUpdatedAt { get; set; }

    public DateTimeOffset? StockUpdatedAt { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? ListPrice { get; set; }

    public string StockText =>
        Quantity.HasValue
            ? Quantity.Value.ToString()
            : "—";

    public string StockStatusText =>
        !Quantity.HasValue
            ? "Stok Kaydı Yok"
            : Quantity.Value == 0
                ? "Tükendi"
                : Quantity.Value <= 5
                    ? "Düşük Stok"
                    : "Stokta";

    public string StockStatusCss =>
        !Quantity.HasValue
            ? "neutral"
            : Quantity.Value == 0
                ? "cancelled"
                : Quantity.Value <= 5
                    ? "pending"
                    : "accepted";
}

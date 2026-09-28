namespace CommerceManager.Web.Models;

public sealed class StockRow
{
    public Guid ProductId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string? StockCode { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public DateTimeOffset StockUpdatedAt { get; set; }

    public string StatusText =>
        Quantity == 0
            ? "Tükendi"
            : Quantity <= 5
                ? "Düşük Stok"
                : "Stokta";

    public string StatusCss =>
        Quantity == 0
            ? "cancelled"
            : Quantity <= 5
                ? "pending"
                : "accepted";
}

public sealed class StockPage
{
    public int Page { get; set; }

    public int Size { get; set; } = 25;

    public int Total { get; set; }

    public int TotalQuantity { get; set; }

    public int InStockCount { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public List<StockRow> Items { get; set; } = [];
}

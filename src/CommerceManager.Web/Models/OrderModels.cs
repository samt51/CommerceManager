namespace CommerceManager.Web.Models;

public sealed class OrderPage
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<OrderRow> Items { get; set; } = [];
}

public sealed class OrderRow
{
    public Guid Id { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset OrderedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string? FirstProductName { get; set; }
    public string? FirstProductImageUrl { get; set; }

    public string StatusText => Status switch
    {
        "Created" => "Oluşturuldu",
        "Picking" => "Hazırlanıyor",
        "Invoiced" => "Faturalandı",
        "Shipped" => "Kargoda",
        "AtCollectionPoint" => "Teslimat Noktasında",
        "Delivered" => "Teslim Edildi",
        "Cancelled" => "İptal Edildi",
        "UnDelivered" => "Teslim Edilemedi",
        "Returned" => "İade Edildi",
        _ => Status ?? "Bilinmiyor"
    };

    public string StatusCss => Status switch
    {
        "Picking" => "pending",
        "Created" => "pending",
        "Invoiced" => "info",
        "Shipped" => "info",
        "AtCollectionPoint" => "info",
        "Delivered" => "accepted",
        "Cancelled" => "cancelled",
        "UnDelivered" => "rejected",
        "Returned" => "rejected",
        _ => "unknown"
    };
}

public sealed class OrderDetails
{
    public Guid Id { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset OrderedAt { get; set; }
    public DateTimeOffset? ExternalLastModifiedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineRow> Items { get; set; } = [];

    public string StatusText => Status switch
    {
        "Created" => "Oluşturuldu",
        "Picking" => "Hazırlanıyor",
        "Invoiced" => "Faturalandı",
        "Shipped" => "Kargoda",
        "AtCollectionPoint" => "Teslimat Noktasında",
        "Delivered" => "Teslim Edildi",
        "Cancelled" => "İptal Edildi",
        "UnDelivered" => "Teslim Edilemedi",
        "Returned" => "İade Edildi",
        _ => Status ?? "Bilinmiyor"
    };

    public string StatusCss => Status switch
    {
        "Picking" => "pending",
        "Created" => "pending",
        "Invoiced" => "info",
        "Shipped" => "info",
        "AtCollectionPoint" => "info",
        "Delivered" => "accepted",
        "Cancelled" => "cancelled",
        "UnDelivered" => "rejected",
        "Returned" => "rejected",
        _ => "unknown"
    };
}

public sealed class OrderLineRow
{
    public string ExternalLineId { get; set; } = "";
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;
}

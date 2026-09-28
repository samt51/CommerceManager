namespace CommerceManager.Web.Models;

public sealed class ReturnPage
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<ReturnRow> Items { get; set; } = [];
}

public sealed class ReturnRow
{
    public Guid Id { get; set; }
    public string ExternalReturnId { get; set; } = "";
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public int Channel { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int Status { get; set; }
    public bool StockApplied { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset ExternalLastModifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string StatusText => Status switch
    {
        1 => "Bekliyor",
        2 => "Kabul Edildi",
        3 => "Reddedildi",
        4 => "İptal Edildi",
        _ => "Bilinmiyor"
    };

    public string StatusCss => Status switch
    {
        1 => "pending",
        2 => "accepted",
        3 => "rejected",
        4 => "cancelled",
        _ => "unknown"
    };

    public string ChannelText => Channel switch
    {
        1 => "Trendyol",
        2 => "Web Sitesi",
        3 => "Mağaza",
        _ => "Diğer"
    };
}

public sealed class ReturnDetails
{
    public Guid Id { get; set; }
    public string ExternalReturnId { get; set; } = "";
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public string? OrderNumber { get; set; }
    public string? ExternalOrderId { get; set; }
    public int Channel { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int Status { get; set; }
    public bool StockApplied { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset ExternalLastModifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    public string StatusText => Status switch
    {
        1 => "Bekliyor",
        2 => "Kabul Edildi",
        3 => "Reddedildi",
        4 => "İptal Edildi",
        _ => "Bilinmiyor"
    };

    public string StatusCss => Status switch
    {
        1 => "pending",
        2 => "accepted",
        3 => "rejected",
        4 => "cancelled",
        _ => "unknown"
    };

    public string ChannelText => Channel switch
    {
        1 => "Trendyol",
        2 => "Web Sitesi",
        3 => "Mağaza",
        _ => "Diğer"
    };
}

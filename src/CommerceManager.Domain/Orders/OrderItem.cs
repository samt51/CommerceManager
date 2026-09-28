namespace CommerceManager.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem() { } // EF Core

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string ExternalLineId { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public static OrderItem Create(
        string externalLineId,
        string? barcode,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(externalLineId))
            throw new ArgumentException("Line ID is required.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        return new OrderItem
        {
            ExternalLineId = externalLineId,
            Barcode = barcode,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : imageUrl.Trim();
    }

    public void Update(
        string? barcode,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        Barcode = barcode;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

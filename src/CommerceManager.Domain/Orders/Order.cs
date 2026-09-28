namespace CommerceManager.Domain.Orders;

public sealed class Order
{
    private Order() { } // EF Core

    public Guid Id { get; private set; } = Guid.NewGuid();
    public SalesChannel Channel { get; private set; }
    public string? ExternalOrderId { get; private set; } // Trendyol: shipmentPackageId
    public string? OrderNumber { get; private set; }
    public string? Status { get; private set; }
    public DateTimeOffset OrderedAt { get; private set; }
    public DateTimeOffset? ExternalLastModifiedAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

    public static Order Create(
        SalesChannel channel,
        string? externalOrderId,
        DateTimeOffset orderedAt,
        decimal totalAmount)
    {
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (channel != SalesChannel.Retail &&
            string.IsNullOrWhiteSpace(externalOrderId))
            throw new ArgumentException(
                "External ID is required for integrated orders.",
                nameof(externalOrderId));

        if (totalAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount));

        return new Order
        {
            Channel = channel,
            ExternalOrderId = externalOrderId,
            OrderedAt = orderedAt,
            TotalAmount = totalAmount
        };
    }

    public void UpdateTrendyolPackage(
        string orderNumber,
        string status,
        DateTimeOffset lastModifiedAt,
        decimal totalAmount)
    {
        if (Channel != SalesChannel.Trendyol)
            throw new InvalidOperationException("Not a Trendyol package.");
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.");
        if (totalAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount));

        OrderNumber = orderNumber;
        Status = status;
        ExternalLastModifiedAt = lastModifiedAt;
        TotalAmount = totalAmount;
    }
}

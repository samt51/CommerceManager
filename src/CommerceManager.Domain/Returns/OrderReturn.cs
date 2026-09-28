namespace CommerceManager.Domain.Returns;

public sealed class OrderReturn
{
    private OrderReturn() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid OrderId { get; private set; }
    public Guid OrderItemId { get; private set; }

    public string ExternalReturnId { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public ReturnStatus Status { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset ExternalLastModifiedAt { get; private set; }

    public bool StockApplied { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } =
        DateTimeOffset.UtcNow;

    public static OrderReturn Create(
        Guid orderId,
        Guid orderItemId,
        string externalReturnId,
        int quantity,
        ReturnStatus status,
        DateTimeOffset externalLastModifiedAt)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.");

        if (orderItemId == Guid.Empty)
            throw new ArgumentException("Order item ID is required.");

        if (string.IsNullOrWhiteSpace(externalReturnId))
            throw new ArgumentException("External return ID is required.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new OrderReturn
        {
            OrderId = orderId,
            OrderItemId = orderItemId,
            ExternalReturnId = externalReturnId,
            Quantity = quantity,
            Status = status,
            ExternalLastModifiedAt = externalLastModifiedAt
        };
    }

    public void Update(
        int quantity,
        ReturnStatus status,
        DateTimeOffset externalLastModifiedAt)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity = quantity;
        Status = status;
        ExternalLastModifiedAt = externalLastModifiedAt;
    }

    public void MarkStockApplied(DateTimeOffset acceptedAt)
    {
        if (Status != ReturnStatus.Accepted)
            throw new InvalidOperationException(
                "Only accepted returns can affect stock.");

        if (StockApplied)
            return;

        StockApplied = true;
        AcceptedAt = acceptedAt;
    }
}

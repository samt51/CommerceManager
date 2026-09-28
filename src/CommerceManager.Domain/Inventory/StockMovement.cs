using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Inventory;

public sealed class StockMovement
{
    private StockMovement() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public SalesChannel Channel { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public StockMovementType Type { get; private set; }

    public int Quantity { get; private set; }

    public string ReferenceType { get; private set; } = string.Empty;

    public string ReferenceId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; } =
        DateTimeOffset.UtcNow;

    public static StockMovement Create(
        SalesChannel channel,
        string barcode,
        StockMovementType type,
        int quantity,
        string referenceType,
        string referenceId)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (string.IsNullOrWhiteSpace(referenceType))
            throw new ArgumentException("Reference type is required.");

        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("Reference ID is required.");

        return new StockMovement
        {
            Channel = channel,
            Barcode = barcode,
            Type = type,
            Quantity = quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        };
    }
}

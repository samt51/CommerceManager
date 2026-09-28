using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Inventory;

public sealed class ChannelStock
{
    private ChannelStock() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public SalesChannel Channel { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; } =
        DateTimeOffset.UtcNow;

    public static ChannelStock Create(
        SalesChannel channel,
        string barcode,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.");

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new ChannelStock
        {
            Channel = channel,
            Barcode = barcode,
            Quantity = quantity
        };
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new InvalidOperationException(
                "Stock quantity cannot be negative.");

        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Increase(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Decrease(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (Quantity < quantity)
            throw new InvalidOperationException(
                "Insufficient channel stock.");

        Quantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

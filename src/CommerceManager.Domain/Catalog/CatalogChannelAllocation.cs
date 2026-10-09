using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Catalog;

public sealed class CatalogChannelAllocation
{
    private CatalogChannelAllocation()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogProductId { get; private set; }

    public SalesChannel Channel { get; private set; }

    public int Quantity { get; private set; }

    public static CatalogChannelAllocation Create(
        Guid catalogProductId,
        SalesChannel channel,
        int quantity)
    {
        if (catalogProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(catalogProductId));

        if (!Enum.IsDefined(channel) || channel == SalesChannel.Retail)
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return new CatalogChannelAllocation
        {
            Id = Guid.NewGuid(),
            CatalogProductId = catalogProductId,
            Channel = channel,
            Quantity = quantity
        };
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity = quantity;
    }

    public void Increase(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity += quantity;
    }

    public void Decrease(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity -= quantity;
    }
}

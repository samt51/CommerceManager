using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Catalog;

public sealed class CatalogStockMovement
{
    private CatalogStockMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogProductId { get; private set; }

    public SalesChannel Channel { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public StockMovementType Type { get; private set; }

    public int Quantity { get; private set; }

    public string ReferenceType { get; private set; } = string.Empty;

    public string ReferenceId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static CatalogStockMovement Create(
        Guid catalogProductId,
        SalesChannel channel,
        string barcode,
        StockMovementType type,
        int quantity,
        string referenceType,
        string referenceId)
    {
        if (catalogProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(catalogProductId));

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (string.IsNullOrWhiteSpace(referenceType))
            throw new ArgumentException("Reference type is required.", nameof(referenceType));

        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("Reference ID is required.", nameof(referenceId));

        return new CatalogStockMovement
        {
            Id = Guid.NewGuid(),
            CatalogProductId = catalogProductId,
            Channel = channel,
            Barcode = barcode.Trim(),
            Type = type,
            Quantity = quantity,
            ReferenceType = referenceType.Trim(),
            ReferenceId = referenceId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

namespace CommerceManager.Domain.Orders;

public sealed class RetailSale
{
    private RetailSale()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? CustomerName { get; private set; }

    public string? CustomerPhone { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static RetailSale Create(
        Guid catalogProductId,
        int quantity,
        decimal unitPrice,
        string? customerName,
        string? customerPhone)
    {
        if (catalogProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(catalogProductId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        return new RetailSale
        {
            Id = Guid.NewGuid(),
            CatalogProductId = catalogProductId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalAmount = unitPrice * quantity,
            CustomerName = Normalize(customerName),
            CustomerPhone = Normalize(customerPhone),
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

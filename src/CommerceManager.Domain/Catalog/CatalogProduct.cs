namespace CommerceManager.Domain.Catalog;

public sealed class CatalogProduct
{
    private CatalogProduct()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal SalePrice { get; private set; }

    public string? ImageUrl { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CatalogProduct Create(
        string name,
        int quantity,
        decimal salePrice = 0,
        string? description = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice));

        var now = DateTimeOffset.UtcNow;

        return new CatalogProduct
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = Normalize(description),
            SalePrice = salePrice,
            ImageUrl = Normalize(imageUrl),
            Quantity = quantity,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetDetails(string name, string? description, decimal salePrice, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice));

        Name = name.Trim();
        Description = Normalize(description);
        SalePrice = salePrice;
        ImageUrl = Normalize(imageUrl);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

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

        Quantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

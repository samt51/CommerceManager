using CommerceManager.Domain.Orders;

namespace CommerceManager.Domain.Catalog;

public sealed class CatalogBarcode
{
    private CatalogBarcode()
    {
    }

    public Guid Id { get; private set; }

    public Guid CatalogProductId { get; private set; }

    public SalesChannel Channel { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public int Units { get; private set; } = 1;

    public decimal? ListPrice { get; private set; }

    public static CatalogBarcode Create(
        Guid catalogProductId,
        SalesChannel channel,
        string barcode,
        int units = 1)
    {
        if (catalogProductId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(catalogProductId));

        if (channel is not (SalesChannel.Trendyol or SalesChannel.Website))
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));

        if (units < 1)
            throw new ArgumentOutOfRangeException(nameof(units));

        return new CatalogBarcode
        {
            Id = Guid.NewGuid(),
            CatalogProductId = catalogProductId,
            Channel = channel,
            Barcode = barcode.Trim(),
            Units = units
        };
    }

    public void SetUnits(int units)
    {
        if (units < 1)
            throw new ArgumentOutOfRangeException(nameof(units));

        Units = units;
    }

    public void SetListPrice(decimal? listPrice)
    {
        if (listPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(listPrice));

        ListPrice = listPrice;
    }
}

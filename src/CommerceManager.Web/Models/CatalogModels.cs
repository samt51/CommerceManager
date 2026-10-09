using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models;

public sealed class CatalogProductRow
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal SalePrice { get; set; }

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public string? TrendyolBarcode { get; set; }

    public string? WebsiteBarcode { get; set; }

    public int RetailQuantity { get; set; }

    public List<CatalogAllocationItem> Allocations { get; set; } = [];

    public List<CatalogBarcodeItem> Barcodes { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public string StockStatusText =>
        Quantity < 0
            ? "Eksi stok"
            : Quantity == 0
                ? "Tükendi"
                : Quantity <= 5
                    ? "Düşük stok"
                    : "Stokta";

    public string StockStatusCss =>
        Quantity < 0
            ? "cancelled"
            : Quantity == 0
                ? "cancelled"
                : Quantity <= 5
                    ? "pending"
                    : "accepted";
}

public sealed class CatalogProductPage
{
    public int Page { get; set; }

    public int Size { get; set; } = 25;

    public int Total { get; set; }

    public int TotalQuantity { get; set; }

    public List<CatalogProductRow> Items { get; set; } = [];
}

public sealed class CatalogBarcodeItem
{
    public int Channel { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public int Units { get; set; } = 1;
}

public sealed class CatalogAllocationItem
{
    public int Channel { get; set; }

    public int Quantity { get; set; }

    public string ChannelName => Channel switch
    {
        1 => "Trendyol",
        2 => "Site",
        _ => $"Kanal {Channel}"
    };
}

public sealed class CreateCatalogProductViewModel
{
    [Required(ErrorMessage = "Ürün adı gerekli.")]
    [MaxLength(300, ErrorMessage = "Ürün adı en fazla 300 karakter olabilir.")]
    [Display(Name = "Ürün adı")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Stok adedi 0 veya daha büyük olmalı.")]
    [Display(Name = "Stok adedi")]
    public int Quantity { get; set; }

    public string? PriceText { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public IFormFile? Image { get; set; }

    [MaxLength(150, ErrorMessage = "Barkod en fazla 150 karakter olabilir.")]
    [Display(Name = "Trendyol barkodu")]
    public string? TrendyolBarcode { get; set; }

    [MaxLength(150, ErrorMessage = "Barkod en fazla 150 karakter olabilir.")]
    [Display(Name = "Site barkodu")]
    public string? WebsiteBarcode { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Trendyol payı 0 veya daha büyük olmalı.")]
    [Display(Name = "Trendyol payı")]
    public int TrendyolAllocation { get; set; }

    public int TrendyolUnits { get; set; } = 1;

    public int WebsiteUnits { get; set; } = 1;

    [Range(0, int.MaxValue, ErrorMessage = "Site payı 0 veya daha büyük olmalı.")]
    [Display(Name = "Site payı")]
    public int WebsiteAllocation { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace CommerceManager.Web.Models;

public sealed class ExpenseTypeItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class ExpenseItem
{
    public Guid Id { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class ExpensePage
{
    public decimal Total { get; set; }

    public List<ExpenseTypeItem> Types { get; set; } = [];

    public List<ExpenseItem> Items { get; set; } = [];
}

public sealed class CreateExpenseViewModel
{
    public List<ExpenseTypeItem> Types { get; set; } = [];

    [Required(ErrorMessage = "Gider tipi seçin.")]
    [Display(Name = "Gider tipi")]
    public Guid? ExpenseTypeId { get; set; }

    [Required(ErrorMessage = "Tutar gerekli.")]
    [Display(Name = "Tutar")]
    public string AmountText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama gerekli.")]
    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;
}

public sealed class CreateExpenseTypeViewModel
{
    [Required(ErrorMessage = "Gider tipi gerekli.")]
    [MaxLength(100)]
    [Display(Name = "Yeni gider tipi")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CashMovementItem
{
    public Guid Id { get; set; }

    public int Direction { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string DirectionText => Direction == 1 ? "Giriş" : "Çıkış";
}

public sealed class CashPage
{
    public decimal Balance { get; set; }

    public decimal TotalIn { get; set; }

    public decimal TotalOut { get; set; }

    public List<CashMovementItem> Items { get; set; } = [];

    public decimal TrendyolRevenue { get; set; }

    public int TrendyolOrders { get; set; }

    public decimal WebsiteRevenue { get; set; }

    public int WebsiteOrders { get; set; }
}

public sealed class AddCashInflowViewModel
{
    [Required(ErrorMessage = "Tutar gerekli.")]
    [Display(Name = "Tutar")]
    public string AmountText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama gerekli.")]
    [MaxLength(500)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;
}

public sealed class RetailSaleForm
{
    public Guid CatalogProductId { get; set; }

    public int Quantity { get; set; } = 1;

    public string UnitPriceText { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }
}

public sealed class RetailSaleItem
{
    public Guid Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class RetailSalePage
{
    public List<RetailSaleItem> Items { get; set; } = [];

    public List<CatalogProductRow> Products { get; set; } = [];
}

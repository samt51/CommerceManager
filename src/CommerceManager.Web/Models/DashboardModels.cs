namespace CommerceManager.Web.Models;

public sealed class BusinessOverviewModel
{
    public decimal RetailRevenue { get; set; }

    public int RetailSales { get; set; }

    public decimal TrendyolRevenue { get; set; }

    public int TrendyolOrders { get; set; }

    public decimal WebsiteRevenue { get; set; }

    public int WebsiteOrders { get; set; }

    public decimal CashBalance { get; set; }

    public decimal TodayExpenses { get; set; }

    public int OnHandQuantity { get; set; }

    public int OutOfStockProducts { get; set; }

    public bool TrendyolFinanceAvailable { get; set; }

    public decimal TrendyolCommission { get; set; }

    public decimal TrendyolDiscount { get; set; }

    public decimal TrendyolSellerRevenue { get; set; }

    public string? ApiError { get; set; }

    public decimal TotalRevenue => RetailRevenue + TrendyolRevenue + WebsiteRevenue;

    public int TotalSales => RetailSales + TrendyolOrders + WebsiteOrders;
}

public sealed class DashboardViewModel
{
    public string SelectedPeriod { get; set; } = "this-month";

    public DashboardSummaryModel Summary { get; set; } = new();

    public List<DashboardRecentOrderModel> RecentOrders { get; set; } = [];

    public List<DashboardStatusItemModel> OrderStatuses { get; set; } = [];

    public List<DashboardDailySalesItemModel> DailySales { get; set; } = [];

    public string? ApiError { get; set; }
}

public sealed class DashboardSummaryModel
{
    public int TotalOrders { get; set; }
    public decimal GrossSales { get; set; }
    public decimal AcceptedReturnAmount { get; set; }
    public decimal NetSales { get; set; }
    public int TotalProducts { get; set; }
    public int TotalStockQuantity { get; set; }
    public int AcceptedReturnCount { get; set; }
    public int PreparingOrderCount { get; set; }
}

public sealed class DashboardRecentOrderModel
{
    public Guid Id { get; set; }
    public string? OrderNumber { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset OrderedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public sealed class DashboardStatusItemModel
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DashboardDailySalesItemModel
{
    public DateOnly Date { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

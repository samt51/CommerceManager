using MediatR;

namespace CommerceManager.Application.Dashboard;

public sealed record DashboardSummary(
    int TotalOrders,
    decimal GrossSales,
    decimal AcceptedReturnAmount,
    decimal NetSales,
    int TotalProducts,
    int TotalStockQuantity,
    int AcceptedReturnCount,
    int PreparingOrderCount);

public sealed record DashboardRecentOrder(
    Guid Id,
    string? OrderNumber,
    string? Status,
    DateTimeOffset OrderedAt,
    decimal TotalAmount,
    int ItemCount);

public sealed record DashboardStatusItem(
    string Status,
    int Count);

public sealed record DashboardDailySalesItem(
    DateOnly Date,
    int OrderCount,
    decimal TotalAmount);

public sealed record DashboardData(
    DashboardSummary Summary,
    IReadOnlyList<DashboardRecentOrder> RecentOrders,
    IReadOnlyList<DashboardStatusItem> OrderStatuses,
    IReadOnlyList<DashboardDailySalesItem> DailySales);

public sealed record BusinessOverview(
    decimal RetailRevenue,
    int RetailSales,
    decimal TrendyolRevenue,
    int TrendyolOrders,
    decimal WebsiteRevenue,
    int WebsiteOrders,
    decimal CashBalance,
    decimal TodayExpenses,
    int OnHandQuantity,
    int OutOfStockProducts,
    bool TrendyolFinanceAvailable,
    decimal TrendyolCommission,
    decimal TrendyolDiscount,
    decimal TrendyolSellerRevenue);

public interface IDashboardReader
{
    Task<DashboardData> GetTrendyolAsync(
        string period,
        CancellationToken cancellationToken);

    Task<BusinessOverview> GetOverviewAsync(
        string period,
        CancellationToken cancellationToken);
}

public sealed record GetBusinessOverview(string Period = "day") : IRequest<BusinessOverview>;

public sealed class GetBusinessOverviewHandler(IDashboardReader reader)
    : IRequestHandler<GetBusinessOverview, BusinessOverview>
{
    public Task<BusinessOverview> Handle(
        GetBusinessOverview request,
        CancellationToken cancellationToken) =>
        reader.GetOverviewAsync(request.Period, cancellationToken);
}

public sealed record GetTrendyolDashboard(
    string Period = "this-month")
    : IRequest<DashboardData>;

public sealed class GetTrendyolDashboardHandler(
    IDashboardReader reader)
    : IRequestHandler<GetTrendyolDashboard, DashboardData>
{
    public Task<DashboardData> Handle(
        GetTrendyolDashboard request,
        CancellationToken cancellationToken)
        => reader.GetTrendyolAsync(
            request.Period,
            cancellationToken);
}

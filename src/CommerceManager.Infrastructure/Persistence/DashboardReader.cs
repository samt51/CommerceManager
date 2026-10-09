using CommerceManager.Application.Dashboard;
using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Cash;
using CommerceManager.Domain.Orders;
using CommerceManager.Domain.Returns;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class DashboardReader(
    AppDbContext db,
    ITrendyolPackageReader trendyolPackages,
    ITrendyolSettlementReader trendyolSettlements)
    : IDashboardReader
{
    public async Task<DashboardData> GetTrendyolAsync(
        string period,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        DateTimeOffset? periodStart = period switch
        {
            "last-30-days" => now.Date.AddDays(-29),
            "last-3-months" => now.Date.AddMonths(-3),
            "last-6-months" => now.Date.AddMonths(-6),
            "this-year" => new DateTimeOffset(
                now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "all-time" => null,
            _ => new DateTimeOffset(
                now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var allOrders = db.Orders
            .AsNoTracking()
            .Where(x => x.Channel == SalesChannel.Trendyol);

        var orders = periodStart.HasValue
            ? allOrders.Where(x => x.OrderedAt >= periodStart.Value)
            : allOrders;

        // Cancelled siparişler satış hesabına dahil edilmez.
        var salesOrders = orders
            .Where(x => x.Status != "Cancelled");

        var totalOrders =
            await orders.CountAsync(cancellationToken);

        var grossSales =
            await salesOrders
                .SumAsync(
                    x => (decimal?)x.TotalAmount,
                    cancellationToken)
            ?? 0m;

        var totalProducts =
            await db.Products
                .AsNoTracking()
                .CountAsync(
                    x => x.Channel == SalesChannel.Trendyol,
                    cancellationToken);

        var totalStockQuantity =
            await db.ChannelStocks
                .AsNoTracking()
                .Where(x => x.Channel == SalesChannel.Trendyol)
                .SumAsync(
                    x => (int?)x.Quantity,
                    cancellationToken)
            ?? 0;

        var preparingOrderCount =
            await orders.CountAsync(
                x => x.Status == "Picking",
                cancellationToken);

        var acceptedReturns =
            from orderReturn in db.OrderReturns.AsNoTracking()
            join order in db.Orders.AsNoTracking()
                on orderReturn.OrderId equals order.Id
            join item in db.OrderItems.AsNoTracking()
                on orderReturn.OrderItemId equals item.Id
            where
                order.Channel == SalesChannel.Trendyol &&
                orderReturn.Status == ReturnStatus.Accepted &&
                (!periodStart.HasValue ||
                 (orderReturn.AcceptedAt.HasValue &&
                  orderReturn.AcceptedAt.Value >= periodStart.Value))
            select new
            {
                orderReturn.Quantity,
                item.UnitPrice
            };

        var acceptedReturnCount =
            await acceptedReturns.CountAsync(
                cancellationToken);

        var acceptedReturnAmount =
            await acceptedReturns
                .SumAsync(
                    x => (decimal?)
                        (x.Quantity * x.UnitPrice),
                    cancellationToken)
            ?? 0m;

        var netSales =
            grossSales - acceptedReturnAmount;

        var recentOrders =
            await orders
                .OrderByDescending(x => x.OrderedAt)
                .ThenByDescending(x => x.Id)
                .Take(8)
                .Select(x => new DashboardRecentOrder(
                    x.Id,
                    x.OrderNumber,
                    x.Status,
                    x.OrderedAt,
                    x.TotalAmount,
                    x.Items.Count))
                .ToListAsync(cancellationToken);

        var orderStatusRows =
            await orders
                .GroupBy(x => x.Status)
                .Select(x => new
                {
                    Status = x.Key,
                    Count = x.Count()
                })
                .ToListAsync(cancellationToken);

        var orderStatuses =
            orderStatusRows
                .Select(x => new DashboardStatusItem(
                    x.Status ?? "Bilinmiyor",
                    x.Count))
                .OrderByDescending(x => x.Count)
                .ToList();

        var chartStartDate =
            periodStart ?? (
                await salesOrders
                    .OrderBy(x => x.OrderedAt)
                    .Select(x => (DateTimeOffset?)x.OrderedAt)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? now.Date);

        var startDate = chartStartDate.Date;

        var dailyRaw =
            await salesOrders
                .Where(x => x.OrderedAt >= startDate)
                .GroupBy(x => x.OrderedAt.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    OrderCount = x.Count(),
                    TotalAmount = x.Sum(y => y.TotalAmount)
                })
                .ToListAsync(cancellationToken);

        var dailyLookup =
            dailyRaw.ToDictionary(
                x => DateOnly.FromDateTime(x.Date),
                x => x);

        var chartEndDate = now.Date;

        var chartDayCount =
            Math.Max(
                1,
                (chartEndDate - startDate).Days + 1);

        var dailySales =
            Enumerable.Range(0, chartDayCount)
                .Select(index =>
                {
                    var date =
                        DateOnly.FromDateTime(
                            startDate.AddDays(index));

                    return dailyLookup.TryGetValue(
                        date,
                        out var row)
                        ? new DashboardDailySalesItem(
                            date,
                            row.OrderCount,
                            row.TotalAmount)
                        : new DashboardDailySalesItem(
                            date,
                            0,
                            0m);
                })
                .ToList();

        var summary =
            new DashboardSummary(
                totalOrders,
                grossSales,
                acceptedReturnAmount,
                netSales,
                totalProducts,
                totalStockQuantity,
                acceptedReturnCount,
                preparingOrderCount);

        return new DashboardData(
            summary,
            recentOrders,
            orderStatuses,
            dailySales);
    }

    public async Task<BusinessOverview> GetOverviewAsync(
        CancellationToken cancellationToken)
    {
        TimeZoneInfo zone;

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.CreateCustomTimeZone(
                "Turkey",
                TimeSpan.FromHours(3),
                "Turkey",
                "Turkey");
        }

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        var start = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            0,
            0,
            0,
            localNow.Offset);
        var end = start.AddDays(1);

        var retail = await db.RetailSales.AsNoTracking()
            .Where(x => x.OccurredAt >= start && x.OccurredAt < end)
            .GroupBy(_ => 1)
            .Select(x => new { Amount = x.Sum(s => s.TotalAmount), Count = x.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var trendyol = await TrendyolTodayAsync(start, cancellationToken);
        var settlement = await trendyolSettlements.ReadRangeAsync(start, end, cancellationToken);
        var website = await ChannelTodayAsync(SalesChannel.Website, start, end, cancellationToken);

        var cashIn = await db.CashMovements.AsNoTracking()
            .Where(x => x.Direction == CashDirection.In)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        var cashOut = await db.CashMovements.AsNoTracking()
            .Where(x => x.Direction == CashDirection.Out)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        var expenses = await db.Expenses.AsNoTracking()
            .Where(x => x.OccurredAt >= start && x.OccurredAt < end)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        var onHand = await db.CatalogProducts.AsNoTracking()
            .SumAsync(x => (int?)x.Quantity, cancellationToken) ?? 0;

        var outOfStock = await db.CatalogProducts.AsNoTracking()
            .CountAsync(x => x.Quantity <= 0, cancellationToken);

        return new BusinessOverview(
            retail?.Amount ?? 0,
            retail?.Count ?? 0,
            trendyol.Amount,
            trendyol.Count,
            website.Amount,
            website.Count,
            cashIn - cashOut,
            expenses,
            onHand,
            outOfStock,
            settlement.Available && (settlement.Commission != 0 || settlement.Discount != 0 || settlement.SellerRevenue != 0),
            settlement.Commission,
            settlement.Discount,
            settlement.SellerRevenue);
    }

    private async Task<(decimal Amount, int Count)> TrendyolTodayAsync(
        DateTimeOffset start,
        CancellationToken cancellationToken)
    {
        var end = start.AddDays(1);
        var startMs = start.ToUnixTimeMilliseconds();
        var endMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var seenPackages = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<string>(StringComparer.Ordinal);
        decimal amount = 0;

        for (var page = 0; page < 10; page++)
        {
            var result = await trendyolPackages.ReadPageAsync(
                page, 200, startMs, endMs, cancellationToken);
            if (result.Packages.Count == 0)
                break;

            foreach (var package in result.Packages)
            {
                if (!seenPackages.Add(package.PackageId))
                    continue;
                if (package.OrderedAt < start || package.OrderedAt >= end)
                    continue;
                if (IsClosedSale(package.Status))
                    continue;

                amount += package.TotalAmount;
                orders.Add(string.IsNullOrWhiteSpace(package.OrderNumber)
                    ? package.PackageId
                    : package.OrderNumber);
            }

            if (page + 1 >= result.TotalPages)
                break;
        }

        return (amount, orders.Count);
    }

    private static bool IsClosedSale(string status) =>
        status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
        || status.Equals("UnSupplied", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Returned", StringComparison.OrdinalIgnoreCase);

    private async Task<(decimal Amount, int Count)> ChannelTodayAsync(
        SalesChannel channel,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        var row = await db.Orders.AsNoTracking()
            .Where(x =>
                x.Channel == channel &&
                x.OrderedAt >= start &&
                x.OrderedAt < end &&
                x.Status != "Cancelled")
            .GroupBy(_ => 1)
            .Select(x => new { Amount = x.Sum(o => o.TotalAmount), Count = x.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return (row?.Amount ?? 0, row?.Count ?? 0);
    }
}

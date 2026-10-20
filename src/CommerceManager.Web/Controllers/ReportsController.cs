using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class ReportsController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? period, CancellationToken cancellationToken)
    {
        if (period is not ("week" or "month"))
            period = "day";

        ViewBag.Period = period;
        var model = new BusinessReportPage();
        var client = httpClientFactory.CreateClient("api");

        try
        {
            model.Overview = await client.GetFromJsonAsync<BusinessOverviewModel>(
                $"api/dashboard/overview?period={period}",
                cancellationToken) ?? new BusinessOverviewModel();

            var cash = await client.GetFromJsonAsync<CashPage>("api/cash", cancellationToken) ?? new CashPage();
            var expenses = await client.GetFromJsonAsync<ExpensePage>("api/expenses", cancellationToken) ?? new ExpensePage();
            var accounts = await client.GetFromJsonAsync<CurrentAccountPage>("api/current-accounts", cancellationToken)
                ?? new CurrentAccountPage();

            var (start, end) = Range(period);
            model.CashBalance = cash.Balance;
            model.CashInPeriod = cash.Items
                .Where(x => x.OccurredAt >= start && x.OccurredAt < end && x.Direction == 1)
                .Sum(x => x.Amount);
            model.CashOutPeriod = cash.Items
                .Where(x => x.OccurredAt >= start && x.OccurredAt < end && x.Direction != 1)
                .Sum(x => x.Amount);
            model.CashMovements = cash.Items
                .Where(x => x.OccurredAt >= start && x.OccurredAt < end)
                .OrderByDescending(x => x.OccurredAt)
                .Take(12)
                .ToList();

            var periodExpenses = expenses.Items
                .Where(x => x.OccurredAt >= start && x.OccurredAt < end)
                .ToList();
            model.ExpenseTotal = periodExpenses.Sum(x => x.Amount);
            model.ExpenseGroups = periodExpenses
                .GroupBy(x => string.IsNullOrWhiteSpace(x.TypeName) ? "Diğer" : x.TypeName)
                .Select(x => new ExpenseGroupRow(x.Key, x.Sum(i => i.Amount), x.Count()))
                .OrderByDescending(x => x.Amount)
                .ToList();

            model.Receivable = accounts.Receivable;
            model.Payable = accounts.Payable;
            model.Accounts = accounts.Items
                .OrderByDescending(x => Math.Abs(x.Balance))
                .Take(12)
                .ToList();
        }
        catch (HttpRequestException)
        {
            model.Overview.ApiError = "Rapor alınırken API bağlantısı kurulamadı.";
        }

        return View(model);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) Range(string period)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3));
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var start = period switch
        {
            "week" => today.AddDays(-6),
            "month" => new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset),
            _ => today
        };
        return (start, today.AddDays(1));
    }
}

public sealed class BusinessReportPage
{
    public BusinessOverviewModel Overview { get; set; } = new();

    public decimal CashBalance { get; set; }

    public decimal CashInPeriod { get; set; }

    public decimal CashOutPeriod { get; set; }

    public List<CashMovementItem> CashMovements { get; set; } = [];

    public decimal ExpenseTotal { get; set; }

    public List<ExpenseGroupRow> ExpenseGroups { get; set; } = [];

    public decimal Receivable { get; set; }

    public decimal Payable { get; set; }

    public List<CurrentAccountRow> Accounts { get; set; } = [];
}

public sealed record ExpenseGroupRow(string Name, decimal Amount, int Count);

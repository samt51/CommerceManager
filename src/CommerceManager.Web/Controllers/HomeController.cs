using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class HomeController(
    IHttpClientFactory httpClientFactory)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string period = "this-month",
        CancellationToken cancellationToken = default)
    {
        var allowedPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "this-month",
            "last-30-days",
            "last-3-months",
            "last-6-months",
            "this-year",
            "all-time"
        };

        if (!allowedPeriods.Contains(period))
            period = "this-month";

        var model = new DashboardViewModel
        {
            SelectedPeriod = period
        };

        try
        {
            var client =
                httpClientFactory.CreateClient("api");

            var result =
                await client.GetFromJsonAsync<DashboardViewModel>(
                    $"api/dashboard/trendyol?period={Uri.EscapeDataString(period)}",
                    cancellationToken);

            if (result is not null)
            {
                model = result;
                model.SelectedPeriod = period;
            }
        }
        catch (HttpRequestException)
        {
            model.ApiError =
                "Dashboard verileri alınırken API bağlantısı kurulamadı.";
        }
        catch (TaskCanceledException) when (
            !cancellationToken.IsCancellationRequested)
        {
            model.ApiError =
                "Dashboard API zaman aşımına uğradı.";
        }

        return View(model);
    }
}

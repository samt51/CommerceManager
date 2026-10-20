using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class HomeController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? period,
        CancellationToken cancellationToken)
    {
        if (period is not ("week" or "month"))
            period = "day";

        ViewBag.Period = period;
        var model = new BusinessOverviewModel();

        try
        {
            model = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<BusinessOverviewModel>($"api/dashboard/overview?period={period}", cancellationToken)
                ?? new BusinessOverviewModel();
        }
        catch (HttpRequestException)
        {
            model.ApiError = "Özet alınırken API bağlantısı kurulamadı.";
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            model.ApiError = "Özet zaman aşımına uğradı.";
        }

        return View(model);
    }
}

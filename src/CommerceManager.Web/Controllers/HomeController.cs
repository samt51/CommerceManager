using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class HomeController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new BusinessOverviewModel();

        try
        {
            model = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<BusinessOverviewModel>("api/dashboard/overview", cancellationToken)
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

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class SettingsController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new IntegrationStatusModel();

        try
        {
            model = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<IntegrationStatusModel>("api/integrations/status", cancellationToken)
                ?? new IntegrationStatusModel();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Ayarlar okunamadı.";
        }
        catch (JsonException)
        {
            ViewBag.ApiError = "Ayarlar okunamadı.";
        }

        return View(model);
    }
}

public sealed class IntegrationStatusModel
{
    public bool Trendyol { get; set; }

    public bool Website { get; set; }

    public string? WebsiteHost { get; set; }
}

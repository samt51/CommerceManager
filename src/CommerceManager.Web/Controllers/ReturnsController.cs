using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class ReturnsController(
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 0,
        int size = 25,
        int? status = null,
        string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var website = string.Equals(channel, "website", StringComparison.OrdinalIgnoreCase);
        ViewBag.Channel = website ? "website" : "trendyol";
        var client = httpClientFactory.CreateClient("api");

        var url = website
            ? $"api/returns/website?page={page}&size={size}"
            : $"api/returns?page={page}&size={size}&channel=Trendyol";

        if (status.HasValue)
            url += $"&status={status.Value}";

        try
        {
            var result =
                await client.GetFromJsonAsync<ReturnPage>(
                    url,
                    cancellationToken);

            ViewBag.Status = status;

            return View(result ?? new ReturnPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "API bağlantısı kurulamadı.";

            ViewBag.Status = status;

            return View(new ReturnPage());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("api");

        try
        {
            using var response = await client.GetAsync(
                $"api/returns/{id}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<ReturnDetails>(
                    cancellationToken: cancellationToken);

            if (result is null)
                return NotFound();

            return View(result);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "İade detayı alınırken API bağlantısı kurulamadı.";

            return View(new ReturnDetails { Id = id });
        }
    }

}

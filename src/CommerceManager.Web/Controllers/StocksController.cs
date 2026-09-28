using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class StocksController(
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 0,
        int size = 25,
        string? search = null,
        string? stockStatus = null,
        CancellationToken cancellationToken = default)
    {
        var client =
            httpClientFactory.CreateClient("api");

        var url =
            $"api/stocks/trendyol?page={page}&size={size}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        if (!string.IsNullOrWhiteSpace(stockStatus))
        {
            url +=
                $"&stockStatus={Uri.EscapeDataString(stockStatus)}";
        }

        try
        {
            var result =
                await client.GetFromJsonAsync<StockPage>(
                    url,
                    cancellationToken);

            ViewBag.Search = search;
            ViewBag.StockStatus = stockStatus;

            return View(result ?? new StockPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Stok bilgileri alınırken API bağlantısı kurulamadı.";

            ViewBag.Search = search;
            ViewBag.StockStatus = stockStatus;

            return View(new StockPage());
        }
    }
}

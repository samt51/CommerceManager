using System.Net;
using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class OrdersController(
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 0,
        int size = 25,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("api");

        var url =
            $"api/orders/trendyol?page={page}&size={size}";

        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";

        try
        {
            var result =
                await client.GetFromJsonAsync<OrderPage>(
                    url,
                    cancellationToken);

            ViewBag.Status = status;

            return View(result ?? new OrderPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Siparişler alınırken API bağlantısı kurulamadı.";

            ViewBag.Status = status;

            return View(new OrderPage());
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
                $"api/orders/trendyol/{id}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<OrderDetails>(
                    cancellationToken: cancellationToken);

            return result is null
                ? NotFound()
                : View(result);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Sipariş detayı alınırken API bağlantısı kurulamadı.";

            return View(new OrderDetails { Id = id });
        }
    }
}

using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class ProductsController(
    IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 0,
        int size = 25,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var client =
            httpClientFactory.CreateClient("api");

        var url =
            $"api/products/trendyol?page={page}&size={size}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        try
        {
            var result =
                await client.GetFromJsonAsync<ProductPage>(
                    url,
                    cancellationToken);

            ViewBag.Search = search;

            return View(result ?? new ProductPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Ürünler alınırken API bağlantısı kurulamadı.";

            ViewBag.Search = search;

            return View(new ProductPage());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client =
            httpClientFactory.CreateClient("api");

        try
        {
            using var response =
                await client.GetAsync(
                    $"api/products/trendyol/{id}",
                    cancellationToken);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();

            var product =
                await response.Content
                    .ReadFromJsonAsync<ProductDetails>(
                        cancellationToken);

            if (product is null)
                return NotFound();

            return View(product);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Ürün detayı alınırken API bağlantısı kurulamadı.";

            return View(new ProductDetails
            {
                Id = id
            });
        }
    }

}

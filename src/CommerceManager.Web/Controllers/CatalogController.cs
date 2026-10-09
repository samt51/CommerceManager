using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class CatalogController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 0,
        int size = 25,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("api");

        var url = $"api/catalog-products?page={page}&size={size}";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        try
        {
            var result = await client.GetFromJsonAsync<CatalogProductPage>(
                url,
                cancellationToken);

            ViewBag.Search = search;

            return View(result ?? new CatalogProductPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Ürünler alınırken API bağlantısı kurulamadı.";
            ViewBag.Search = search;

            return View(new CatalogProductPage());
        }
    }

    [HttpGet]
    public IActionResult Create() =>
        View(new CreateCatalogProductViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCatalogProductViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ProductError"] = "Ürün adı gerekli. Stok ve paylar 0 veya daha büyük olmalı.";
            TempData["OpenModal"] = "product";
            return RedirectToAction(nameof(Index));
        }

        var client = httpClientFactory.CreateClient("api");

        try
        {
            var response = await client.PostAsJsonAsync(
                "api/catalog-products",
                new
                {
                    model.Name,
                    model.Quantity,
                    SalePrice = 0m,
                    model.TrendyolBarcode,
                    model.WebsiteBarcode,
                    Allocations = new object[]
                    {
                        new { Channel = 1, Quantity = model.TrendyolAllocation },
                        new { Channel = 2, Quantity = model.WebsiteAllocation }
                    }
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken);
                TempData["ProductError"] = error?.Message ?? "Ürün kaydedilemedi.";
                TempData["OpenModal"] = "product";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (HttpRequestException)
        {
            TempData["ProductError"] = "Ürün kaydedilirken API bağlantısı kurulamadı.";
            TempData["OpenModal"] = "product";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Ürün genel stoka eklendi.";

        return RedirectToAction(nameof(Index));
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}

using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class SalesController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CancellationToken cancellationToken)
    {
        ViewBag.Search = search;
        var client = httpClientFactory.CreateClient("api");
        var model = new RetailSalePage();

        try
        {
            model.Items = await client.GetFromJsonAsync<List<RetailSaleItem>>(
                "api/retail-sales",
                cancellationToken) ?? [];

            model.Products = await LoadProductsAsync(client, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Satışlar alınırken API bağlantısı kurulamadı.";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            model.Items = model.Items
                .Where(x =>
                    x.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.CustomerName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.CustomerPhone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        RetailSaleForm model,
        CancellationToken cancellationToken)
    {
        if (model.CatalogProductId == Guid.Empty ||
            model.Quantity <= 0 ||
            !TurkishMoney.TryParse(model.UnitPriceText, out var unitPrice))
        {
            TempData["SaleError"] = "Ürün, adet ve birim fiyat gerekli. Fiyat örnek: 199,90";
            TempData["OpenModal"] = "sale";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    "api/retail-sales",
                    new
                    {
                        model.CatalogProductId,
                        model.Quantity,
                        UnitPrice = unitPrice,
                        model.CustomerName,
                        model.CustomerPhone
                    },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
                TempData["SaleError"] = error?.Message ?? "Satış kaydedilemedi.";
                TempData["OpenModal"] = "sale";
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Perakende satış kaydedildi. Stok düştü, tutar kasaya girdi.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["SaleError"] = "Satış kaydedilirken API bağlantısı kurulamadı.";
            TempData["OpenModal"] = "sale";
        }

        return RedirectToAction(nameof(Index));
    }

    private static async Task<List<CatalogProductRow>> LoadProductsAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var products = new List<CatalogProductRow>();

        for (var page = 0; page < 20; page++)
        {
            var result = await client.GetFromJsonAsync<CatalogProductPage>(
                $"api/catalog-products?page={page}&size=100",
                cancellationToken);

            if (result?.Items is null || result.Items.Count == 0)
                break;

            products.AddRange(result.Items);

            if (products.Count >= result.Total)
                break;
        }

        return products.OrderBy(x => x.Name).ToList();
    }

    private sealed class ApiError
    {
        public string? Message { get; set; }
    }
}

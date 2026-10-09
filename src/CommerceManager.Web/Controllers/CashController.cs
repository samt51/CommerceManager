using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class CashController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Products = new List<CatalogProductRow>();

        try
        {
            ViewBag.Products = await LoadProductsAsync(cancellationToken);

            var page = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<CashPage>("api/cash", cancellationToken);

            return View(page ?? new CashPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Kasa alınırken API bağlantısı kurulamadı.";
            ViewBag.Products = new List<CatalogProductRow>();
            return View(new CashPage());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInflow(
        AddCashInflowViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid ||
            !TurkishMoney.TryParse(model.AmountText, out var amount))
        {
            TempData["CashError"] = "Kasa girişi için tutar ve açıklama gerekli. Tutar örnek: 1.000,50";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    "api/cash/inflows",
                    new { Amount = amount, model.Description },
                    cancellationToken);

            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "CashError"] =
                response.IsSuccessStatusCode
                    ? "Kasa girişi eklendi."
                    : "Kasa girişi kaydedilemedi.";
        }
        catch (HttpRequestException)
        {
            TempData["CashError"] = "Kasa girişi kaydedilirken API bağlantısı kurulamadı.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sell(
        RetailSaleForm model,
        CancellationToken cancellationToken)
    {
        if (model.CatalogProductId == Guid.Empty ||
            model.Quantity <= 0 ||
            !TurkishMoney.TryParse(model.UnitPriceText, out var unitPrice))
        {
            TempData["CashError"] = "Ürün, adet ve birim fiyat gerekli. Fiyat örnek: 199,90";
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
                        CustomerName = model.CustomerName,
                        CustomerPhone = model.CustomerPhone
                    },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
                TempData["CashError"] = error?.Message ?? "Satış kaydedilemedi.";
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
            TempData["CashError"] = "Satış kaydedilirken API bağlantısı kurulamadı.";
            TempData["OpenModal"] = "sale";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<CatalogProductRow>> LoadProductsAsync(
        CancellationToken cancellationToken)
    {
        var products = new List<CatalogProductRow>();
        var client = httpClientFactory.CreateClient("api");

        try
        {
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
        }
        catch (HttpRequestException)
        {
            return [];
        }

        return products.OrderBy(x => x.Name).ToList();
    }

    private sealed class ApiError
    {
        public string? Message { get; set; }
    }
}

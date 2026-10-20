using System.Net.Http.Json;
using System.Text.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class ProductsController(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment) : Controller
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
            $"api/catalog-products?page={page}&size={size}";

        if (!string.IsNullOrWhiteSpace(search))
        {
            url +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        try
        {
            var result =
                await client.GetFromJsonAsync<CatalogProductPage>(
                    url,
                    cancellationToken);

            ViewBag.Search = search;

            return View(result ?? new CatalogProductPage());
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError =
                "Ürünler alınırken API bağlantısı kurulamadı.";

            ViewBag.Search = search;

            return View(new CatalogProductPage());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCatalogProductViewModel model,
        string? barcodesJson,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid ||
            !TurkishMoney.TryParse(model.PriceText, out var salePrice) ||
            salePrice <= 0)
        {
            TempData["ProductError"] = "Ürün adı ve satış fiyatı gerekli. Fiyat örnek: 199,90";
            TempData["OpenModal"] = "product";
            return RedirectToAction(nameof(Index));
        }

        var imageUrl = await SaveImageAsync(model.Image);

        if (model.Image is { Length: > 0 } && imageUrl is null)
        {
            TempData["ProductError"] = "Resim jpg, png veya webp olmalı.";
            TempData["OpenModal"] = "product";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    "api/catalog-products",
                    new
                    {
                        model.Name,
                        model.Quantity,
                        SalePrice = salePrice,
                        model.Description,
                        ImageUrl = imageUrl,
                        TrendyolBarcode = (string?)null,
                        WebsiteBarcode = (string?)null,
                        TrendyolUnits = 1,
                        WebsiteUnits = 1,
                        Barcodes = ParseBarcodes(barcodesJson),
                        Allocations = new object[]
                        {
                            new { Channel = 1, Quantity = model.TrendyolAllocation }
                        }
                    },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
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

        TempData["SuccessMessage"] = "Ürün eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> TrendyolLive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await httpClientFactory.CreateClient("api")
                .GetFromJsonAsync<JsonElement>($"api/catalog-products/{id}/trendyol-live", cancellationToken);
            return Json(result);
        }
        catch (HttpRequestException)
        {
            return Json(new { items = Array.Empty<object>() });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid id,
        string name,
        string? description,
        string? priceText,
        string? imageUrl,
        IFormFile? image,
        int quantity,
        int trendyolQuantity,
        string? trendyolListPriceText,
        int websiteQuantity,
        string? trendyolBarcode,
        int trendyolUnits,
        string? websiteBarcode,
        int websiteUnits,
        string? packBarcodesJson,
        bool sendToTrendyol,
        CancellationToken cancellationToken)
    {
        if (!TurkishMoney.TryParse(priceText, out var salePrice) || salePrice < 0)
        {
            TempData["ProductError"] = "Satış fiyatı gerekli. Örnek: 199,90";
            return RedirectToAction(nameof(Index));
        }

        var savedImage = await SaveImageAsync(image);

        if (image is { Length: > 0 } && savedImage is null)
        {
            TempData["ProductError"] = "Resim jpg, png veya webp olmalı.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    $"api/catalog-products/{id}",
                    new
                    {
                        Name = name,
                        Description = description,
                        SalePrice = salePrice,
                        ImageUrl = savedImage ?? imageUrl,
                        Quantity = quantity,
                        TrendyolQuantity = trendyolQuantity,
                        TrendyolListPrice = TurkishMoney.TryParse(trendyolListPriceText, out var listPrice) && listPrice > 0
                            ? listPrice
                            : (decimal?)null,
                        WebsiteQuantity = websiteQuantity,
                        SendToTrendyol = sendToTrendyol,
                        Barcodes = BuildBarcodes(
                            trendyolBarcode,
                            trendyolUnits,
                            websiteBarcode,
                            websiteUnits,
                            packBarcodesJson)
                    },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
                TempData["ProductError"] = error?.Message ?? "Ürün güncellenemedi.";
            }
            else
            {
                TempData["SuccessMessage"] = "Ürün kaydedildi. Trendyol barkodlarının stoğu gönderildi.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["ProductError"] = "Ürün güncellenirken API bağlantısı kurulamadı.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushTrendyol(
        Guid catalogProductId,
        int quantity,
        bool send,
        CancellationToken cancellationToken)
    {
        if (catalogProductId == Guid.Empty)
        {
            TempData["ProductError"] = "Ürün seçilmedi.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await httpClientFactory.CreateClient("api")
                .PostAsJsonAsync(
                    $"api/catalog-products/{catalogProductId}/trendyol-stock",
                    new { Quantity = quantity, Send = send },
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(cancellationToken);
                TempData["ProductError"] = error?.Message ?? "Trendyol stoğu kaydedilemedi.";
            }
            else if (send)
            {
                TempData["SuccessMessage"] =
                    "Trendyol stoğu kaydedildi ve barkodla Trendyol'a gönderildi.";
            }
            else
            {
                TempData["SuccessMessage"] = "Trendyol stoğu kaydedildi. Henüz gönderilmedi.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["ProductError"] = "Trendyol stoğu kaydedilirken API bağlantısı kurulamadı.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Trendyol(
        int page = 0,
        int size = 25,
        string? search = null,
        string? channel = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 0)
            page = 0;
        if (size is < 1 or > 100)
            size = 25;

        var model = new IntegrationCatalogPage
        {
            Page = page,
            Size = size,
            Channel = channel
        };
        ViewBag.Search = search;

        try
        {
            var client = httpClientFactory.CreateClient("api");
            var trendyol = new List<ProductRow>();
            for (var index = 0; index < 8; index++)
            {
                var chunk = await client.GetFromJsonAsync<ProductPage>(
                    $"api/products/trendyol?page={index}&size=100",
                    cancellationToken);
                if (chunk is null || chunk.Items.Count == 0)
                    break;
                trendyol.AddRange(chunk.Items);
                if (trendyol.Count >= chunk.Total)
                    break;
            }

            IReadOnlyList<WebsiteProductRow> website = [];
            try
            {
                website = await client.GetFromJsonAsync<List<WebsiteProductRow>>(
                    "api/products/website",
                    cancellationToken) ?? [];
            }
            catch (HttpRequestException)
            {
                model.WebsiteNote = "E-ticaret ürünleri şu an okunamadı.";
            }

            var rows = trendyol.Select(x => new IntegrationProductRow
            {
                Id = x.Id,
                Channel = "Trendyol",
                Name = x.Name,
                Barcode = x.Barcode,
                StockCode = x.StockCode,
                ImageUrl = x.ImageUrl,
                Quantity = x.Quantity,
                UpdatedAt = x.UpdatedAt
            }).Concat(website.Select(x => new IntegrationProductRow
            {
                Channel = "E-ticaret",
                Name = x.Name,
                Barcode = x.Barcode,
                ImageUrl = x.ImageUrl,
                Quantity = x.Quantity,
                Price = x.Price
            }));

            model.TrendyolCount = trendyol.Count;
            model.WebsiteCount = website.Count;

            if (string.Equals(channel, "trendyol", StringComparison.OrdinalIgnoreCase))
                rows = rows.Where(x => x.Channel == "Trendyol");
            else if (string.Equals(channel, "website", StringComparison.OrdinalIgnoreCase))
                rows = rows.Where(x => x.Channel == "E-ticaret");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                rows = rows.Where(x =>
                    x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    x.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.StockCode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var list = rows.OrderBy(x => x.Name).ThenBy(x => x.Barcode).ToList();
            model.Total = list.Count;
            model.Items = list.Skip(page * size).Take(size).ToList();
            return View(model);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "Entegrasyon ürünleri alınırken API bağlantısı kurulamadı.";
            return View(model);
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

    private static List<CatalogBarcodeItem> ParseBarcodes(string? barcodesJson)
    {
        if (string.IsNullOrWhiteSpace(barcodesJson))
            return [];

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<CatalogBarcodeItem>>(
                barcodesJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static List<object> BuildBarcodes(
        string? trendyolBarcode,
        int trendyolUnits,
        string? websiteBarcode,
        int websiteUnits,
        string? packBarcodesJson)
    {
        var barcodes = new List<object>();

        if (!string.IsNullOrWhiteSpace(trendyolBarcode))
            barcodes.Add(new { Channel = 1, Barcode = trendyolBarcode.Trim(), Units = Math.Max(trendyolUnits, 1) });

        if (!string.IsNullOrWhiteSpace(websiteBarcode))
            barcodes.Add(new { Channel = 2, Barcode = websiteBarcode.Trim(), Units = Math.Max(websiteUnits, 1) });

        if (string.IsNullOrWhiteSpace(packBarcodesJson))
            return barcodes;

        try
        {
            var packs = System.Text.Json.JsonSerializer.Deserialize<List<CatalogBarcodeItem>>(
                packBarcodesJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (packs is null)
                return barcodes;

            foreach (var pack in packs)
            {
                if (string.IsNullOrWhiteSpace(pack.Barcode))
                    continue;

                barcodes.Add(new
                {
                    pack.Channel,
                    Barcode = pack.Barcode.Trim(),
                    Units = Math.Max(pack.Units, 1)
                });
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return barcodes;
    }

    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null || image.Length == 0)
            return null;

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            return null;

        var directory = Path.Combine(environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, fileName);

        await using var stream = System.IO.File.Create(path);
        await image.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private sealed class ApiError
    {
        public string? Message { get; set; }
    }
}

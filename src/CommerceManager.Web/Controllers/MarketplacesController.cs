using System.Net.Http.Json;
using CommerceManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Web.Controllers;

public sealed class MarketplacesController(
    IHttpClientFactory httpClientFactory)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model = await BuildModelAsync(cancellationToken);

        model.Message = TempData["MarketplaceMessage"] as string;
        model.IsError =
            string.Equals(
                TempData["MarketplaceError"] as string,
                "true",
                StringComparison.OrdinalIgnoreCase);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncOrders(
        CancellationToken cancellationToken)
    {
        return await ExecuteSyncAsync<OrderSyncResult>(
            "api/integrations/trendyol/orders/sync",
            result =>
                $"Sipariş senkronizasyonu tamamlandı. " +
                $"{result.Received} alındı, " +
                $"{result.Inserted} eklendi, " +
                $"{result.Updated} güncellendi.",
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncProducts(
        CancellationToken cancellationToken)
    {
        return await ExecuteSyncAsync<ProductSyncResult>(
            "api/integrations/trendyol/products/sync",
            result =>
                $"Ürün senkronizasyonu tamamlandı. " +
                $"{result.Received} alındı, " +
                $"{result.Inserted} eklendi, " +
                $"{result.Updated} güncellendi, " +
                $"{result.Unchanged} değişmedi, " +
                $"{result.CatalogInserted} genel stoka eklendi.",
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncInventory(
        CancellationToken cancellationToken)
    {
        return await ExecuteSyncAsync<InventorySyncResult>(
            "api/integrations/trendyol/inventory/sync",
            result =>
                $"Stok senkronizasyonu tamamlandı. " +
                $"{result.Received} alındı, " +
                $"{result.Inserted} yeni stok eklendi, " +
                $"{result.Skipped} mevcut stok korundu.",
            cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncReturns(
        CancellationToken cancellationToken)
    {
        return await ExecuteSyncAsync<ReturnSyncResult>(
            "api/integrations/trendyol/returns/sync?days=30",
            result =>
                $"İade senkronizasyonu tamamlandı. " +
                $"{result.Received} alındı, " +
                $"{result.Inserted} eklendi, " +
                $"{result.Updated} güncellendi, " +
                $"{result.StockApplied} stok hareketi uygulandı, " +
                $"{result.OrderNotFound} sipariş bulunamadı, " +
                $"{result.OrderItemNotFound} sipariş kalemi bulunamadı.",
            cancellationToken);
    }

    private async Task<IActionResult> ExecuteSyncAsync<T>(
        string url,
        Func<T, string> successMessage,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var client =
                httpClientFactory.CreateClient("api");

            using var response =
                await client.PostAsync(
                    url,
                    content: null,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<T>(
                        cancellationToken);

            if (result is null)
                throw new InvalidOperationException(
                    "API boş senkronizasyon sonucu döndürdü.");

            TempData["MarketplaceMessage"] =
                successMessage(result);

            TempData["MarketplaceError"] = "false";
        }
        catch (Exception ex)
        {
            TempData["MarketplaceMessage"] =
                $"Senkronizasyon başarısız: {ex.Message}";

            TempData["MarketplaceError"] = "true";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<MarketplaceIndexViewModel>
        BuildModelAsync(
            CancellationToken cancellationToken)
    {
        var model =
            new MarketplaceIndexViewModel();

        try
        {
            var client =
                httpClientFactory.CreateClient("api");

            var integrationStatus =
                await client.GetFromJsonAsync<TrendyolIntegrationStatusResult>(
                    "api/integrations/trendyol/status",
                    cancellationToken);

            if (integrationStatus is not null)
            {
                model.IsActive =
                    integrationStatus.IsConnected;

                model.LastSuccessfulSyncAt =
                    integrationStatus.LastSuccessfulSyncAt;

                model.LastFailedSyncAt =
                    integrationStatus.LastFailedSyncAt;

                model.LastError =
                    integrationStatus.LastError;

                model.LastOrderSyncAt =
                    integrationStatus.LastOrderSyncAt;

                model.LastReturnSyncAt =
                    integrationStatus.LastReturnSyncAt;

                model.LastProductSyncAt =
                    integrationStatus.LastProductSyncAt;

                model.LastInventorySyncAt =
                    integrationStatus.LastInventorySyncAt;
            }

            var products =
                await client.GetFromJsonAsync<ProductPage>(
                    "api/products/trendyol?page=0&size=1",
                    cancellationToken);

            if (products is not null)
                model.ProductCount = products.Total;

            var stocks =
                await client.GetFromJsonAsync<StockPage>(
                    "api/stocks/trendyol?page=0&size=1",
                    cancellationToken);

            if (stocks is not null)
            {
                model.StockCount = stocks.Total;
                model.TotalStockQuantity =
                    stocks.TotalQuantity;
            }
        }
        catch (HttpRequestException)
        {
            model.IsActive = false;
        }

        return model;
    }
}

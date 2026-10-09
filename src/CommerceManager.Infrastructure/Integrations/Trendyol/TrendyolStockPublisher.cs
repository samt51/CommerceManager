using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolStockPublisher(
    HttpClient httpClient,
    IConfiguration configuration)
    : ITrendyolStockPublisher
{
    public async Task<TrendyolStockPushResult> PushQuantityAsync(
        string barcode,
        int quantity,
        decimal? salePrice,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return new TrendyolStockPushResult(false, "Barkod gerekli.", null);

        if (quantity < 0)
            return new TrendyolStockPushResult(false, "Stok 0 veya daha büyük olmalı.", null);

        var sellerId = configuration["Trendyol:SellerId"]
            ?? throw new InvalidOperationException("Trendyol:SellerId is missing.");
        var apiKey = configuration["Trendyol:ApiKey"]
            ?? throw new InvalidOperationException("Trendyol:ApiKey is missing.");
        var apiSecret = configuration["Trendyol:ApiSecret"]
            ?? throw new InvalidOperationException("Trendyol:ApiSecret is missing.");

        var url =
            "https://apigw.trendyol.com/integration/inventory/" +
            $"sellers/{sellerId}/products/price-and-inventory";

        var item = salePrice is null or <= 0
            ? (object)new { barcode = barcode.Trim(), quantity }
            : new { barcode = barcode.Trim(), quantity, salePrice, listPrice = salePrice };

        var payload = JsonSerializer.Serialize(new { items = new[] { item } });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.TryAddWithoutValidation("User-Agent", $"{sellerId} - SelfIntegration");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new TrendyolStockPushResult(
                false,
                $"Trendyol stok güncellemesi kabul etmedi. ({(int)response.StatusCode})",
                null);
        }

        string? batchId = null;

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("batchRequestId", out var batch))
                batchId = batch.GetString();
        }
        catch (JsonException)
        {
        }

        return new TrendyolStockPushResult(true, null, batchId);
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolInventoryReader(
    HttpClient httpClient,
    IConfiguration configuration)
    : ITrendyolInventoryReader
{
    public async Task<TrendyolInventorySnapshot?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException(
                "Barcode is required.",
                nameof(barcode));

        var document = await SendAsync(
            $"?barcode={Uri.EscapeDataString(barcode)}&size=10&page=0",
            cancellationToken);

        using (document)
        {
            var items = ParseItems(document.RootElement);

            return items.FirstOrDefault(
                x => string.Equals(
                    x.Barcode,
                    barcode,
                    StringComparison.Ordinal));
        }
    }

    public async Task<TrendyolInventoryPage> ReadPageAsync(
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        if (page < 0)
            throw new ArgumentOutOfRangeException(nameof(page));

        if (size is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(size));

        var document = await SendAsync(
            $"?page={page}&size={size}",
            cancellationToken);

        using (document)
        {
            var root = document.RootElement;

            var items = ParseItems(root);

            var responsePage =
                root.TryGetProperty("page", out var pageElement)
                    ? pageElement.GetInt32()
                    : page;

            var responseSize =
                root.TryGetProperty("size", out var sizeElement)
                    ? sizeElement.GetInt32()
                    : size;

            var totalElements =
                root.TryGetProperty(
                    "totalElements",
                    out var totalElementsElement)
                    ? totalElementsElement.GetInt32()
                    : items.Count;

            var totalPages =
                root.TryGetProperty(
                    "totalPages",
                    out var totalPagesElement)
                    ? totalPagesElement.GetInt32()
                    : 1;

            var nextPageToken =
                root.TryGetProperty(
                    "nextPageToken",
                    out var tokenElement) &&
                tokenElement.ValueKind == JsonValueKind.String
                    ? tokenElement.GetString()
                    : null;

            return new TrendyolInventoryPage(
                responsePage,
                responseSize,
                totalElements,
                totalPages,
                nextPageToken,
                items);
        }
    }

    private async Task<JsonDocument> SendAsync(
        string queryString,
        CancellationToken cancellationToken)
    {
        var sellerId =
            configuration["Trendyol:SellerId"]
            ?? throw new InvalidOperationException(
                "Trendyol:SellerId is missing.");

        var apiKey =
            configuration["Trendyol:ApiKey"]
            ?? throw new InvalidOperationException(
                "Trendyol:ApiKey is missing.");

        var apiSecret =
            configuration["Trendyol:ApiSecret"]
            ?? throw new InvalidOperationException(
                "Trendyol:ApiSecret is missing.");

        var url =
            $"https://apigw.trendyol.com/integration/product/" +
            $"sellers/{sellerId}/products/approved/inventory-and-price" +
            queryString;

        using var request =
            new HttpRequestMessage(HttpMethod.Get, url);

        var credentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{apiKey}:{apiSecret}"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            $"{sellerId} - SelfIntegration");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Trendyol inventory request failed. " +
                $"Status={(int)response.StatusCode}, Body={body}");
        }

        return JsonDocument.Parse(body);
    }

    private static List<TrendyolInventorySnapshot> ParseItems(
        JsonElement root)
    {
        var result =
            new List<TrendyolInventorySnapshot>();

        if (!root.TryGetProperty(
                "content",
                out var content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var product in content.EnumerateArray())
        {
            if (!product.TryGetProperty(
                    "variants",
                    out var variants) ||
                variants.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var variant in variants.EnumerateArray())
            {
                var barcode =
                    variant.TryGetProperty(
                        "barcode",
                        out var barcodeElement)
                        ? barcodeElement.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(barcode))
                    continue;

                var quantity =
                    variant.TryGetProperty(
                        "quantity",
                        out var quantityElement)
                        ? quantityElement.GetInt32()
                        : 0;

                var stockCode =
                    variant.TryGetProperty(
                        "stockCode",
                        out var stockCodeElement)
                        ? stockCodeElement.GetString()
                        : null;

                result.Add(
                    new TrendyolInventorySnapshot(
                        barcode,
                        quantity,
                        stockCode));
            }
        }

        return result;
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolProductReader(
    HttpClient httpClient,
    IConfiguration configuration)
    : ITrendyolProductReader
{
    public async Task<TrendyolProductSnapshot?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var page = await SendAsync(
            $"?barcode={Uri.EscapeDataString(barcode)}&size=10&page=0",
            cancellationToken);

        return page.Items.FirstOrDefault(
            x => string.Equals(
                x.Barcode,
                barcode,
                StringComparison.Ordinal));
    }

    public Task<TrendyolProductPage> ReadPageAsync(
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        if (page < 0)
            throw new ArgumentOutOfRangeException(nameof(page));

        if (size is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(size));

        return SendAsync(
            $"?page={page}&size={size}",
            cancellationToken);
    }

    private async Task<TrendyolProductPage> SendAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var sellerId = Required("Trendyol:SellerId");
        var apiKey = Required("Trendyol:ApiKey");
        var apiSecret = Required("Trendyol:ApiSecret");

        var url =
            "https://apigw.trendyol.com/integration/product/" +
            $"sellers/{sellerId}/products/approved{query}";

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
                $"Trendyol product request failed. " +
                $"Status={(int)response.StatusCode}, Body={body}");
        }

        using var document = JsonDocument.Parse(body);

        var root = document.RootElement;

        var page = GetInt32(root, "page");
        var size = GetInt32(root, "size");
        var totalElements = GetInt32(root, "totalElements");
        var totalPages = GetInt32(root, "totalPages");

        var nextPageToken =
            root.TryGetProperty(
                "nextPageToken",
                out var tokenElement) &&
            tokenElement.ValueKind == JsonValueKind.String
                ? tokenElement.GetString()
                : null;

        var items =
            new List<TrendyolProductSnapshot>();

        if (root.TryGetProperty(
                "content",
                out var content) &&
            content.ValueKind == JsonValueKind.Array)
        {
            foreach (var product in content.EnumerateArray())
            {
                ParseProduct(product, items);
            }
        }

        return new TrendyolProductPage(
            page,
            size,
            totalElements,
            totalPages,
            nextPageToken,
            items);
    }

    private static void ParseProduct(
        JsonElement product,
        ICollection<TrendyolProductSnapshot> items)
    {
        var title =
            GetString(product, "title");

        var externalProductId =
            GetValueAsString(product, "contentId");

        var productMainId =
            GetValueAsString(product, "productMainId");

        string? imageUrl = null;

        if (product.TryGetProperty(
                "images",
                out var images) &&
            images.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in images.EnumerateArray())
            {
                imageUrl =
                    GetString(image, "url");

                if (!string.IsNullOrWhiteSpace(imageUrl))
                    break;
            }
        }

        if (!product.TryGetProperty(
                "variants",
                out var variants) ||
            variants.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var variant in variants.EnumerateArray())
        {
            var barcode =
                GetString(variant, "barcode");

            if (string.IsNullOrWhiteSpace(barcode))
                continue;

            var stockCode =
                GetString(variant, "stockCode");

            items.Add(
                new TrendyolProductSnapshot(
                    barcode,
                    title,
                    imageUrl,
                    stockCode,
                    externalProductId,
                    productMainId,
                    GetInt32(variant, "quantity")));
        }
    }

    private static int GetInt32(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var number))
        {
            return number;
        }

        return 0;
    }

    private static string? GetString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetValueAsString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private string Required(string key)
        => configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Missing configuration: {key}");
}

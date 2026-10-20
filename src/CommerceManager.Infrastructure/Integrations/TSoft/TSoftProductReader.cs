using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed record TSoftProductRow(
    string Barcode,
    string Name,
    string? ImageUrl,
    int Quantity,
    decimal Price);

public sealed class TSoftProductReader(
    TSoftClient client,
    IConfiguration configuration,
    IMemoryCache cache)
{
    public async Task<IReadOnlyList<TSoftProductRow>> ReadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["TSoft:SiteUrl"]) ||
            string.IsNullOrWhiteSpace(configuration["TSoft:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["TSoft:Password"]))
            return [];

        if (cache.TryGetValue("tsoft-products-v4", out IReadOnlyList<TSoftProductRow>? cached) && cached is not null)
            return cached;

        try
        {
            var token = await client.LoginAsync(cancellationToken);
            var rows = new List<TSoftProductRow>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var start = 0; start < 2000; start += 100)
            {
                using var page = await client.PostAsync(
                    "product/getProducts",
                    token,
                    new Dictionary<string, string>
                    {
                        ["start"] = start.ToString(CultureInfo.InvariantCulture),
                        ["limit"] = "100",
                        ["columns"] = "ProductName,Barcode,Stock,SellingPrice,SellingPriceVatIncluded,ImageUrl,ImageUrls,IsActive"
                    },
                    cancellationToken);

                if (!page.RootElement.TryGetProperty("data", out var data) ||
                    data.ValueKind != JsonValueKind.Array ||
                    data.GetArrayLength() == 0)
                    break;

                foreach (var item in data.EnumerateArray())
                {
                    if (!IsActive(item))
                        continue;
                    var barcode = Text(item, "Barcode");
                    if (string.IsNullOrWhiteSpace(barcode) || !seen.Add(barcode))
                        continue;

                    var price = Money(item, "SellingPriceVatIncluded");
                    if (price <= 0)
                        price = Money(item, "SellingPrice");
                    if (price <= 0)
                        continue;

                    rows.Add(new TSoftProductRow(
                        barcode,
                        Text(item, "ProductName") ?? barcode,
                        Image(item),
                        Math.Max(0, Int(item, "Stock")),
                        price));
                }

                if (data.GetArrayLength() < 100)
                    break;
            }

            cache.Set("tsoft-products-v4", rows, TimeSpan.FromMinutes(2));
            return rows;
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? Image(JsonElement item)
    {
        if (item.TryGetProperty("ImageUrls", out var urls))
        {
            if (urls.ValueKind == JsonValueKind.String &&
                urls.GetString() is { Length: > 0 } raw &&
                raw.TrimStart().StartsWith('['))
            {
                using var parsed = JsonDocument.Parse(raw);
                var full = FirstImage(parsed.RootElement);
                if (full is not null)
                    return full;
            }

            var direct = FirstImage(urls);
            if (direct is not null)
                return direct;
        }

        var relative = Text(item, "ImageUrl");
        if (string.IsNullOrWhiteSpace(relative))
            return null;
        if (relative.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return relative;
        return "https://zegparhome.tsoftstatic.com/Data/K/" + relative.TrimStart('/');
    }

    private static string? FirstImage(JsonElement urls)
    {
        if (urls.ValueKind != JsonValueKind.Array || urls.GetArrayLength() == 0)
            return null;
        var full = Text(urls[0], "ImageUrl");
        return string.IsNullOrWhiteSpace(full) ? null : full;
    }

    private static bool IsActive(JsonElement item)
    {
        var value = Text(item, "IsActive");
        return value is null or "1" or "true";
    }

    private static string? Text(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static int Int(JsonElement item, string name)
    {
        var text = Text(item, name);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static decimal Money(JsonElement item, string name)
    {
        var text = Text(item, name);
        if (text is null)
            return 0;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return value;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value)
            ? value
            : 0;
    }
}

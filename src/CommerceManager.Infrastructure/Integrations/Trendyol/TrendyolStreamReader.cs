using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolStreamReader(
    HttpClient client, IConfiguration configuration) : ITrendyolStreamReader
{
    public async Task<TrendyolStreamPage> ReadAsync(
        long startMs, long endMs, string? cursor, CancellationToken cancellationToken)
    {
        var sellerId = Required("Trendyol:SellerId");
        var key = Required("Trendyol:ApiKey");
        var secret = Required("Trendyol:ApiSecret");
        var url = $"https://apigw.trendyol.com/integration/order/sellers/{Uri.EscapeDataString(sellerId)}/orders/stream" +
            $"?lastModifiedStartDate={startMs}&lastModifiedEndDate={endMs}&size=100" +
            (cursor is null ? "" : $"&nextCursor={Uri.EscapeDataString(cursor)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));
        request.Headers.TryAddWithoutValidation("User-Agent", $"{sellerId} - SelfIntegration");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var packages = new List<TrendyolPackageSnapshot>();
        foreach (var item in root.GetProperty("content").EnumerateArray())
        {
            var lines = new List<TrendyolLineSnapshot>();
            foreach (var line in item.GetProperty("lines").EnumerateArray())
                lines.Add(new TrendyolLineSnapshot(
                    Text(line, "lineId"), OptionalText(line, "barcode"),
                    OptionalText(line, "productName") ?? "",
                    line.GetProperty("quantity").GetInt32(),
                    line.GetProperty("lineUnitPrice").GetDecimal()));
            packages.Add(new TrendyolPackageSnapshot(
                Text(item, "shipmentPackageId"), Text(item, "orderNumber"),
                OptionalText(item, "shipmentPackageStatus") ?? OptionalText(item, "status") ?? "Unknown",
                DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("orderDate").GetInt64()),
                DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("lastModifiedDate").GetInt64()),
                item.GetProperty("packageTotalPrice").GetDecimal(), lines));
        }
        var hasMore = root.GetProperty("hasMore").GetBoolean();
        return new TrendyolStreamPage(packages, hasMore,
            OptionalText(root, "nextCursor"));
    }
    private string Required(string key) => configuration[key] is { Length: > 0 } value
        ? value : throw new InvalidOperationException($"Missing configuration: {key}");
    private static string Text(JsonElement element, string property) =>
        OptionalText(element, property) ?? throw new JsonException($"Missing {property}.");
    private static string? OptionalText(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }
}

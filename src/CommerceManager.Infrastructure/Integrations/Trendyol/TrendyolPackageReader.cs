using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolPackageReader(
    HttpClient client, IConfiguration configuration) : ITrendyolPackageReader
{
    public async Task<TrendyolPackagePage> ReadPageAsync(
        int page, int size, long startMs, long endMs, CancellationToken cancellationToken)
    {
        var sellerId = Required("Trendyol:SellerId");
        var key = Required("Trendyol:ApiKey");
        var secret = Required("Trendyol:ApiSecret");
        var url = $"https://apigw.trendyol.com/integration/order/sellers/{Uri.EscapeDataString(sellerId)}/v2/orders" +
            $"?startDate={startMs}&endDate={endMs}&page={page}&size={size}" +
            "&orderByField=PackageLastModifiedDate&orderByDirection=DESC";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));
        request.Headers.TryAddWithoutValidation("User-Agent", $"{sellerId} - SelfIntegration");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
        var content = json.RootElement.GetProperty("content");
        var packages = new List<TrendyolPackageSnapshot>();
        foreach (var item in content.EnumerateArray())
        {
            var lines = new List<TrendyolLineSnapshot>();
            foreach (var line in item.GetProperty("lines").EnumerateArray())
                lines.Add(new TrendyolLineSnapshot(
                    Text(line, "lineId"), OptionalText(line, "barcode"),
                    OptionalText(line, "productName") ?? "", line.GetProperty("quantity").GetInt32(),
                    line.GetProperty("lineUnitPrice").GetDecimal()));
            packages.Add(new TrendyolPackageSnapshot(
                Text(item, "shipmentPackageId"), Text(item, "orderNumber"),
                OptionalText(item, "shipmentPackageStatus") ?? OptionalText(item, "status") ?? "Unknown",
                DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("orderDate").GetInt64()),
                DateTimeOffset.FromUnixTimeMilliseconds(item.GetProperty("lastModifiedDate").GetInt64()),
                item.GetProperty("packageTotalPrice").GetDecimal(), lines));
        }
        return new TrendyolPackagePage(packages,
            json.RootElement.GetProperty("totalPages").GetInt32());
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

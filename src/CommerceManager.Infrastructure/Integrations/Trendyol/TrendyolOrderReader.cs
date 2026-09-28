using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolOrderReader(
    HttpClient httpClient,
    IConfiguration configuration) : ITrendyolOrderReader
{
    public async Task<TrendyolOrderPreview> GetPreviewAsync(
        CancellationToken cancellationToken)
    {
        var sellerId = Required("Trendyol:SellerId");
        var apiKey = Required("Trendyol:ApiKey");
        var apiSecret = Required("Trendyol:ApiSecret");

        var end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var start = DateTimeOffset.UtcNow.AddDays(-1)
            .ToUnixTimeMilliseconds();

        var url =
            $"https://apigw.trendyol.com/integration/order/sellers/{Uri.EscapeDataString(sellerId)}/v2/orders" +
            $"?startDate={start}&endDate={end}&page=0&size=10" +
            "&orderByField=PackageLastModifiedDate&orderByDirection=DESC";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.TryAddWithoutValidation(
            "User-Agent", $"{sellerId} - SelfIntegration");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(
            request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var json = await JsonDocument.ParseAsync(
            stream, cancellationToken: cancellationToken);

        var root = json.RootElement;
        return new TrendyolOrderPreview(
            ReadInt(root, "totalElements"),
            ReadInt(root, "totalPages"),
            ReadInt(root, "page"),
            root.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array
                ? content.GetArrayLength()
                : 0);
    }

    private string Required(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Missing configuration: {key}");

    private static int ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) &&
        value.TryGetInt32(out var number)
            ? number
            : 0;
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolReturnReader(
    HttpClient client,
    IConfiguration configuration) : ITrendyolReturnReader
{
    public async Task<TrendyolReturnPage> ReadPageAsync(
        int page,
        int size,
        long startMs,
        long endMs,
        CancellationToken cancellationToken)
    {
        var sellerId = Required("Trendyol:SellerId");
        var key = Required("Trendyol:ApiKey");
        var secret = Required("Trendyol:ApiSecret");

        var url =
            $"https://apigw.trendyol.com/integration/order/sellers/{Uri.EscapeDataString(sellerId)}/claims" +
            $"?startDate={startMs}&endDate={endMs}&page={page}&size={size}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{key}:{secret}")));

        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            $"{sellerId} - SelfIntegration");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        using var response =
            await client.SendAsync(request, cancellationToken);

        var responseText =
            await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Trendyol claims request failed. " +
                $"Status={(int)response.StatusCode} {response.StatusCode}. " +
                $"Body={responseText}");
        }

        using var json = JsonDocument.Parse(responseText);

        var root = json.RootElement;

        var returns = new List<TrendyolReturnSnapshot>();

        if (root.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array)
        {
            foreach (var claim in content.EnumerateArray())
            {
                var items =
                    new List<TrendyolReturnItemSnapshot>();

                if (claim.TryGetProperty("items", out var claimGroups) &&
                    claimGroups.ValueKind == JsonValueKind.Array)
                {
                    foreach (var group in claimGroups.EnumerateArray())
                    {
                        string? orderLineId = null;
                        string? barcode = null;
                        var productName = string.Empty;

                        if (group.TryGetProperty(
                                "orderLine",
                                out var orderLine))
                        {
                            orderLineId =
                                OptionalText(orderLine, "id");

                            barcode =
                                OptionalText(orderLine, "barcode");

                            productName =
                                OptionalText(orderLine, "productName")
                                ?? string.Empty;
                        }

                        if (!group.TryGetProperty(
                                "claimItems",
                                out var claimItems) ||
                            claimItems.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (var claimItem in
                                 claimItems.EnumerateArray())
                        {
                            var status = "Unknown";

                            if (claimItem.TryGetProperty(
                                    "claimItemStatus",
                                    out var statusElement))
                            {
                                status =
                                    OptionalText(
                                        statusElement,
                                        "name")
                                    ?? "Unknown";
                            }

                            items.Add(
                                new TrendyolReturnItemSnapshot(
                                    Text(claimItem, "id"),
                                    orderLineId ?? string.Empty,
                                    OptionalText(
                                        claimItem,
                                        "orderLineItemId")
                                    ?? string.Empty,
                                    barcode,
                                    productName,
                                    status));
                        }
                    }
                }

                returns.Add(
                    new TrendyolReturnSnapshot(
                        OptionalText(claim, "claimId")
                            ?? Text(claim, "id"),
                        Text(claim, "orderNumber"),
                        OptionalText(
                            claim,
                            "orderShipmentPackageId"),
                        ReadTimestamp(
                            claim,
                            "claimDate"),
                        ReadTimestamp(
                            claim,
                            "lastModifiedDate"),
                        items));
            }
        }

        return new TrendyolReturnPage(
            returns,
            ReadInt(root, "totalPages"),
            ReadInt(root, "totalElements"));
    }

    private string Required(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Missing configuration: {key}");

    private static DateTimeOffset ReadTimestamp(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
            return DateTimeOffset.UnixEpoch;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt64(out var milliseconds))
            return DateTimeOffset
                .FromUnixTimeMilliseconds(milliseconds);

        if (value.ValueKind == JsonValueKind.String &&
            long.TryParse(
                value.GetString(),
                out milliseconds))
            return DateTimeOffset
                .FromUnixTimeMilliseconds(milliseconds);

        return DateTimeOffset.UnixEpoch;
    }

    private static int ReadInt(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var number))
            return number;

        return 0;
    }

    private static string Text(
        JsonElement element,
        string property) =>
        OptionalText(element, property)
        ?? throw new JsonException(
            $"Missing {property}.");

    private static string? OptionalText(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value) ||
            value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();
    }
}

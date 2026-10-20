using System.Globalization;
using System.Text.Json;
using CommerceManager.Domain.Inventory;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Catalog;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed record TSoftOrderRow(
    string Code,
    string Status,
    DateTimeOffset OrderedAt,
    decimal Total,
    int ItemCount,
    string? ProductName,
    string? ImageUrl);

public sealed record TSoftReturnRow(
    string OrderCode,
    string Barcode,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    DateTimeOffset OrderedAt,
    string Status);

public sealed class TSoftOrderReader(
    TSoftClient client,
    IConfiguration configuration,
    IMemoryCache cache,
    AppDbContext db,
    CatalogStockApplier stock)
{
    public async Task<IReadOnlyList<TSoftOrderRow>> OrdersAsync(CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(cancellationToken);
        return rows.Orders;
    }

    public async Task<IReadOnlyList<TSoftReturnRow>> ReturnsAsync(CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(cancellationToken);
        return rows.Returns;
    }

    private async Task<(IReadOnlyList<TSoftOrderRow> Orders, IReadOnlyList<TSoftReturnRow> Returns)> LoadAsync(
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue("tsoft-orders-v1", out (List<TSoftOrderRow> Orders, List<TSoftReturnRow> Returns) cached))
            return cached;

        if (string.IsNullOrWhiteSpace(configuration["TSoft:SiteUrl"]))
            return ([], []);

        try
        {
            var token = await client.LoginAsync(cancellationToken);
            var orders = new List<TSoftOrderRow>();
            var returns = new List<TSoftReturnRow>();

            for (var start = 0; start < 400; start += 50)
            {
                using var page = await client.PostAsync(
                    "order2/getOrders",
                    token,
                    new Dictionary<string, string>
                    {
                        ["start"] = start.ToString(CultureInfo.InvariantCulture),
                        ["limit"] = "50"
                    },
                    cancellationToken);

                if (!page.RootElement.TryGetProperty("data", out var data) ||
                    data.ValueKind != JsonValueKind.Array ||
                    data.GetArrayLength() == 0)
                    break;

                foreach (var item in data.EnumerateArray())
                {
                    var code = Text(item, "OrderCode") ?? Text(item, "OrderId");
                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    var status = Text(item, "OrderStatus") ?? "Bilinmiyor";
                    var cancelled = status.Contains("iptal", StringComparison.OrdinalIgnoreCase);
                    var when = When(item) ?? DateTimeOffset.UtcNow;
                    var lines = item.TryGetProperty("OrderDetails", out var details) &&
                                details.ValueKind == JsonValueKind.Array
                        ? details.EnumerateArray().ToList()
                        : [];
                    var first = lines.FirstOrDefault();
                    orders.Add(new TSoftOrderRow(
                        code,
                        status,
                        when,
                        Money(item, "OrderTotalPrice"),
                        Math.Max(lines.Count, 1),
                        first.ValueKind == JsonValueKind.Undefined ? null : Text(first, "ProductName"),
                        first.ValueKind == JsonValueKind.Undefined ? null : Text(first, "ImageUrl")));

                    if (status.Contains("iade", StringComparison.OrdinalIgnoreCase))
                    {
                        returns.Add(new TSoftReturnRow(
                            code,
                            first.ValueKind == JsonValueKind.Undefined ? "" : Text(first, "Barcode") ?? "",
                            first.ValueKind == JsonValueKind.Undefined ? status : Text(first, "ProductName") ?? status,
                            first.ValueKind == JsonValueKind.Undefined ? null : Text(first, "ImageUrl"),
                            1,
                            when,
                            status));
                    }

                    if (!cancelled)
                    {
                        foreach (var line in lines)
                        {
                            var barcode = Text(line, "Barcode");
                            var pieces = Math.Max(1, Int(line, "Quantity"));
                            if (!string.IsNullOrWhiteSpace(barcode))
                            {
                                await stock.TryApplyAsync(
                                    SalesChannel.Website,
                                    barcode,
                                    pieces,
                                    StockMovementType.Sale,
                                    "TSoftOrder",
                                    code,
                                    cancellationToken);
                            }
                        }
                    }

                    foreach (var line in lines)
                    {
                        if (Int(line, "RefundCount") <= 0)
                            continue;
                        returns.Add(new TSoftReturnRow(
                            code,
                            Text(line, "Barcode") ?? "",
                            Text(line, "ProductName") ?? code,
                            Text(line, "ImageUrl"),
                            Math.Max(1, Int(line, "RefundCount")),
                            when,
                            "İade"));
                    }
                }

                if (data.GetArrayLength() < 50)
                    break;
            }

            await db.SaveChangesAsync(cancellationToken);
            var result = (orders, returns);
            cache.Set("tsoft-orders-v1", result, TimeSpan.FromMinutes(1));
            return result;
        }
        catch (HttpRequestException)
        {
            return ([], []);
        }
        catch (InvalidOperationException)
        {
            return ([], []);
        }
        catch (JsonException)
        {
            return ([], []);
        }
    }

    private static string? Text(JsonElement item, string name)
    {
        if (item.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;
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
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static DateTimeOffset? When(JsonElement item)
    {
        var text = Text(item, "OrderDate");
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }
}

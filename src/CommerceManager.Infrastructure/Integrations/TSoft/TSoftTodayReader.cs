using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed class TSoftTodayReader(TSoftClient client, IConfiguration configuration)
{
    public async Task<(decimal Amount, int Count)> ReadAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["TSoft:SiteUrl"]) ||
            string.IsNullOrWhiteSpace(configuration["TSoft:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["TSoft:Password"]))
            return (0, 0);

        try
        {
            var token = await client.LoginAsync(cancellationToken);
            var orders = new HashSet<string>(StringComparer.Ordinal);
            decimal amount = 0;

            for (var startRow = 0; startRow < 400; startRow += 50)
            {
                using var page = await client.PostAsync(
                    "order2/getOrders",
                    token,
                    new Dictionary<string, string>
                    {
                        ["start"] = startRow.ToString(CultureInfo.InvariantCulture),
                        ["limit"] = "50"
                    },
                    cancellationToken);

                if (!page.RootElement.TryGetProperty("data", out var data) ||
                    data.ValueKind != JsonValueKind.Array ||
                    data.GetArrayLength() == 0)
                    break;

                var anyTodayOrNewer = false;
                foreach (var item in data.EnumerateArray())
                {
                    var when = When(item);
                    if (when is null || when < start)
                        continue;
                    anyTodayOrNewer = true;
                    if (when >= end || IsCancelled(item))
                        continue;

                    var code = Text(item, "OrderCode") ?? Text(item, "OrderId");
                    if (string.IsNullOrWhiteSpace(code) || !orders.Add(code))
                        continue;

                    var total = Money(item, "OrderTotalPrice");
                    if (total == 0)
                        total = Money(item, "OrderTotal");
                    amount += total;
                }

                if (!anyTodayOrNewer || data.GetArrayLength() < 50)
                    break;
            }

            return (amount, orders.Count);
        }
        catch (HttpRequestException)
        {
            return (0, 0);
        }
        catch (InvalidOperationException)
        {
            return (0, 0);
        }
        catch (JsonException)
        {
            return (0, 0);
        }
    }

    private static bool IsCancelled(JsonElement item)
    {
        var status = Text(item, "OrderStatus") ?? Text(item, "OrderStatusId") ?? "";
        return status.Contains("iptal", StringComparison.OrdinalIgnoreCase)
            || status.Contains("cancel", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Text(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
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

    private static DateTimeOffset? When(JsonElement item)
    {
        var text = Text(item, "OrderDate");
        if (text is null)
            return null;

        var turkey = TimeSpan.FromHours(3);
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), turkey);

        return DateTime.TryParse(text, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), turkey)
            : null;
    }
}

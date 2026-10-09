using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommerceManager.Application.Integrations.Trendyol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommerceManager.Infrastructure.Integrations.Trendyol;

public sealed class TrendyolSettlementReader(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TrendyolSettlementReader> logger)
    : ITrendyolSettlementReader
{
    public async Task<TrendyolSettlementSummary> ReadRangeAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerId = configuration["Trendyol:SellerId"];
            var apiKey = configuration["Trendyol:ApiKey"];
            var apiSecret = configuration["Trendyol:ApiSecret"];

            if (string.IsNullOrWhiteSpace(sellerId) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                return new TrendyolSettlementSummary(false, 0, 0, 0);
            }

            decimal commission = 0;
            decimal discount = 0;
            decimal revenue = 0;
            var anyPage = false;
            var types = new[] { "Sale", "Return", "Discount", "Coupon" };

            foreach (var transactionType in types)
            {
                for (var page = 0; page < 5; page++)
                {
                    var url =
                        "https://apigw.trendyol.com/integration/finance/che/" +
                        $"sellers/{sellerId}/settlements" +
                        $"?startDate={start.ToUnixTimeMilliseconds()}" +
                        $"&endDate={end.ToUnixTimeMilliseconds()}" +
                        $"&transactionType={transactionType}&page={page}&size=500";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var credentials = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    request.Headers.TryAddWithoutValidation("User-Agent", $"{sellerId} - SelfIntegration");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    using var response = await httpClient.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                        return new TrendyolSettlementSummary(false, 0, 0, 0);

                    anyPage = true;
                    await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
                    if (!json.RootElement.TryGetProperty("content", out var content))
                        break;

                    foreach (var row in content.EnumerateArray())
                    {
                        if (!OrderedToday(row, start, end))
                            continue;

                        if (transactionType is "Discount" or "Coupon")
                        {
                            var amount = ReadDecimal(row, "debt");
                            if (amount == 0)
                                amount = Math.Abs(ReadDecimal(row, "sellerRevenue"));
                            discount += amount;
                            continue;
                        }

                        commission += ReadDecimal(row, "commissionAmount");
                        revenue += ReadDecimal(row, "sellerRevenue");
                    }

                    var totalPages = json.RootElement.TryGetProperty("totalPages", out var pages)
                        ? pages.GetInt32()
                        : 1;
                    if (page + 1 >= totalPages || content.GetArrayLength() == 0)
                        break;
                }
            }

            if (!anyPage)
                return new TrendyolSettlementSummary(false, 0, 0, 0);

            return new TrendyolSettlementSummary(true, commission, discount, revenue);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Trendyol hakediş özeti alınamadı.");
            return new TrendyolSettlementSummary(false, 0, 0, 0);
        }
    }

    private static bool OrderedToday(JsonElement row, DateTimeOffset start, DateTimeOffset end)
    {
        if (!row.TryGetProperty("orderDate", out var value))
            return false;

        DateTimeOffset at;
        if (value.ValueKind == JsonValueKind.Number)
            at = DateTimeOffset.FromUnixTimeMilliseconds(value.GetInt64());
        else if (value.ValueKind != JsonValueKind.String ||
                 !DateTimeOffset.TryParse(value.GetString(), out at))
            return false;

        return at >= start && at < end;
    }

    private static decimal ReadDecimal(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value))
            return 0;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0
        };
    }
}

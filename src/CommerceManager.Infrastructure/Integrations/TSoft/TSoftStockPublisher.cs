using System.Text.Json;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed class TSoftStockPublisher(TSoftClient client)
{
    public async Task<(bool Success, string? Error)> PushAsync(
        string barcode,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode) || quantity < 0)
            return (false, "T-Soft için barkod ve stok gerekli.");

        try
        {
            var token = await client.LoginAsync(cancellationToken);
            var data = JsonSerializer.Serialize(new[]
            {
                new { Barcode = barcode.Trim(), Stock = quantity.ToString() }
            });
            using var page = await client.PostAsync(
                "product/updateStock",
                token,
                new Dictionary<string, string> { ["data"] = data },
                cancellationToken);

            var success = page.RootElement.TryGetProperty("success", out var flag) &&
                (flag.ValueKind == JsonValueKind.True ||
                 (flag.ValueKind == JsonValueKind.String &&
                  flag.GetString() is "true" or "1"));
            return success
                ? (true, null)
                : (false, "T-Soft stoğu kabul etmedi.");
        }
        catch (HttpRequestException)
        {
            return (false, "T-Soft stok servisine ulaşılamadı.");
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }
    }
}

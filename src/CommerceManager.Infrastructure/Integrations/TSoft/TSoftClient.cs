using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CommerceManager.Infrastructure.Integrations.TSoft;

public sealed class TSoftClient(HttpClient http, IConfiguration configuration)
{
    public async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var site = Site();
        var user = Required("TSoft:Username");
        var pass = Required("TSoft:Password");
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["pass"] = pass
        });
        using var response = await http.PostAsync(
            $"{site}/rest1/auth/login/{Uri.EscapeDataString(user)}",
            body,
            cancellationToken);
        var json = await ReadJsonAsync(response, cancellationToken);
        var token = json.RootElement.GetProperty("data")[0].GetProperty("token").GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("T-Soft giriş jetonu boş.");
        return token;
    }

    public async Task<JsonDocument> PostAsync(
        string path,
        string token,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>(fields) { ["token"] = token };
        using var body = new FormUrlEncodedContent(data);
        using var response = await http.PostAsync($"{Site()}/rest1/{path}", body, cancellationToken);
        return await ReadJsonAsync(response, cancellationToken);
    }

    public string Site() => Required("TSoft:SiteUrl").TrimEnd('/');

    private string Required(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Eksik ayar: {key}");

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || !text.TrimStart().StartsWith('{'))
            throw new InvalidOperationException(
                text.Length > 180 ? text[..180] : text);
        return JsonDocument.Parse(text);
    }
}

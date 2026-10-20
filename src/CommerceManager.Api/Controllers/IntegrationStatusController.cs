using Microsoft.AspNetCore.Mvc;

namespace CommerceManager.Api.Controllers;

[ApiController]
[Route("api/integrations/status")]
public sealed class IntegrationStatusController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var site = configuration["TSoft:SiteUrl"];
        string? host = null;
        if (Uri.TryCreate(site, UriKind.Absolute, out var uri))
            host = uri.Host;

        return Ok(new
        {
            trendyol = !string.IsNullOrWhiteSpace(configuration["Trendyol:SellerId"]),
            website = !string.IsNullOrWhiteSpace(configuration["TSoft:Username"]),
            websiteHost = host
        });
    }
}

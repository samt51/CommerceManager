using CommerceManager.Api.Users;
using CommerceManager.Api.Profile;
using CommerceManager.Api.Auth;
using CommerceManager.Application;
using CommerceManager.Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CommerceManager.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<UserAuthService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddHttpClient<CommerceManager.Application.Integrations.Trendyol.ITrendyolOrderReader, CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolOrderReader>();
builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolPackageReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolPackageReader>();
builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolPackageStore,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolPackageStore>();
builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolStreamReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolStreamReader>();

builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolReturnReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolReturnReader>();

builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolInventoryReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolInventoryReader>();

builder.Services.AddHttpClient<CommerceManager.Infrastructure.Integrations.TSoft.TSoftClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CommerceManager");
});

builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolStockPublisher,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolStockPublisher>();

builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolSettlementReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolSettlementReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolInventoryStore,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolInventoryStore>();

builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolProductStore,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolProductStore>();

builder.Services.AddHttpClient<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolProductReader,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolProductReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolProductImageStore,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolProductImageStore>();

builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolReturnStore,
    CommerceManager.Infrastructure.Integrations.Trendyol.TrendyolReturnStore>();
builder.Services.AddScoped<
    CommerceManager.Application.Orders.IOrderReader,
    CommerceManager.Infrastructure.Persistence.OrderReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Inventory.IStockReader,
    CommerceManager.Infrastructure.Persistence.StockReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Products.IProductReader,
    CommerceManager.Infrastructure.Persistence.ProductReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Returns.IReturnReader,
    CommerceManager.Infrastructure.Persistence.ReturnReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Dashboard.IDashboardReader,
    CommerceManager.Infrastructure.Persistence.DashboardReader>();

builder.Services.AddScoped<
    CommerceManager.Application.Integrations.Trendyol.ITrendyolIntegrationStatusReader,
    CommerceManager.Infrastructure.Persistence.TrendyolIntegrationStatusReader>();

builder.Services.AddScoped<
    CommerceManager.Infrastructure.Integrations.MarketplaceIntegrationStatusStore>();
builder.Services.AddHostedService<
    CommerceManager.Api.Integrations.Trendyol.TrendyolOrderSyncWorker>();

builder.Services.AddHostedService<
    CommerceManager.Api.Integrations.Trendyol.TrendyolReturnSyncWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var expectedApiKey = app.Configuration["InternalApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(expectedApiKey))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("API security configuration is missing.");
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedApiKey) ||
                !string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

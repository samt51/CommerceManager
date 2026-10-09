using CommerceManager.Application.Cash;
using CommerceManager.Application.Catalog;
using CommerceManager.Application.Ledger;
using CommerceManager.Application.Orders;
using CommerceManager.Infrastructure.Catalog;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace CommerceManager.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("CommerceManager") ?? throw new InvalidOperationException("ConnectionStrings:CommerceManager is required.");
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<IOrderStore, OrderStore>();
        services.AddScoped<ICatalogProductStore, CatalogProductStore>();
        services.AddScoped<ICatalogProductReader, CatalogProductReader>();
        services.AddScoped<CatalogStockApplier>();
        services.AddScoped<ICurrentAccountStore, CurrentAccountStore>();
        services.AddScoped<ICurrentAccountReader, CurrentAccountReader>();
        services.AddScoped<IExpenseStore, ExpenseStore>();
        services.AddScoped<IExpenseReader, ExpenseReader>();
        services.AddScoped<ICashStore, CashStore>();
        services.AddScoped<ICashReader, CashReader>();
        services.AddScoped<CommerceManager.Infrastructure.Integrations.TSoft.TSoftCatalogSync>();
        services.AddScoped<RetailSaleStore>();
        services.AddScoped<CommerceManager.Infrastructure.Catalog.TrendyolAllocationPublisher>();
        return services;
    }
}

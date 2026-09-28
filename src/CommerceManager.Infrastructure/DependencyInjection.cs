using CommerceManager.Application.Orders;
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
        return services;
    }
}

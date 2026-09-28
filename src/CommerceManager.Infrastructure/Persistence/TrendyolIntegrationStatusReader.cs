using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Persistence;

public sealed class TrendyolIntegrationStatusReader(
    AppDbContext db)
    : ITrendyolIntegrationStatusReader
{
    public Task<TrendyolIntegrationStatus?> GetAsync(
        CancellationToken cancellationToken)
    {
        return db.MarketplaceIntegrationStatuses
            .AsNoTracking()
            .Where(x => x.Channel == SalesChannel.Trendyol)
            .Select(x => new TrendyolIntegrationStatus(
                x.IsConnected,
                x.LastSuccessfulSyncAt,
                x.LastFailedSyncAt,
                x.LastError,
                x.LastOrderSyncAt,
                x.LastReturnSyncAt,
                x.LastProductSyncAt,
                x.LastInventorySyncAt,
                x.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

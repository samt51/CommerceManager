using CommerceManager.Domain.Integrations;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceManager.Infrastructure.Integrations;

public sealed class MarketplaceIntegrationStatusStore(
    AppDbContext db)
{
    public Task MarkOrderSyncSucceededAsync(
        SalesChannel channel,
        CancellationToken cancellationToken)
        => UpdateAsync(
            channel,
            x => x.MarkOrderSyncSucceeded(),
            cancellationToken);

    public Task MarkReturnSyncSucceededAsync(
        SalesChannel channel,
        CancellationToken cancellationToken)
        => UpdateAsync(
            channel,
            x => x.MarkReturnSyncSucceeded(),
            cancellationToken);

    public Task MarkProductSyncSucceededAsync(
        SalesChannel channel,
        CancellationToken cancellationToken)
        => UpdateAsync(
            channel,
            x => x.MarkProductSyncSucceeded(),
            cancellationToken);

    public Task MarkInventorySyncSucceededAsync(
        SalesChannel channel,
        CancellationToken cancellationToken)
        => UpdateAsync(
            channel,
            x => x.MarkInventorySyncSucceeded(),
            cancellationToken);

    public Task MarkFailedAsync(
        SalesChannel channel,
        string? error,
        CancellationToken cancellationToken)
        => UpdateAsync(
            channel,
            x => x.MarkFailed(error),
            cancellationToken);

    private async Task UpdateAsync(
        SalesChannel channel,
        Action<MarketplaceIntegrationStatus> update,
        CancellationToken cancellationToken)
    {
        var status =
            await db.MarketplaceIntegrationStatuses
                .SingleOrDefaultAsync(
                    x => x.Channel == channel,
                    cancellationToken);

        if (status is null)
        {
            status =
                MarketplaceIntegrationStatus.Create(channel);

            db.MarketplaceIntegrationStatuses.Add(status);
        }

        update(status);

        await db.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolProductSyncResult(
    int Received,
    int Inserted,
    int Updated,
    int Unchanged,
    int CatalogInserted);

public interface ITrendyolProductStore
{
    Task<TrendyolProductSyncResult> SyncAsync(
        CancellationToken cancellationToken);
}

public sealed record SyncTrendyolProducts
    : IRequest<TrendyolProductSyncResult>;

public sealed class SyncTrendyolProductsHandler(
    ITrendyolProductStore store)
    : IRequestHandler<
        SyncTrendyolProducts,
        TrendyolProductSyncResult>
{
    public Task<TrendyolProductSyncResult> Handle(
        SyncTrendyolProducts request,
        CancellationToken cancellationToken)
        => store.SyncAsync(cancellationToken);
}

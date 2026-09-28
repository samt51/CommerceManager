using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public interface ITrendyolProductImageStore
{
    Task<TrendyolProductImageBackfillResult> BackfillAsync(
        CancellationToken cancellationToken);
}

public sealed record TrendyolProductImageBackfillResult(
    int UniqueBarcodes,
    int ProductsFound,
    int ProductsNotFound,
    int OrderItemsUpdated);

public sealed record BackfillTrendyolProductImages
    : IRequest<TrendyolProductImageBackfillResult>;

public sealed class BackfillTrendyolProductImagesHandler(
    ITrendyolProductImageStore store)
    : IRequestHandler<
        BackfillTrendyolProductImages,
        TrendyolProductImageBackfillResult>
{
    public Task<TrendyolProductImageBackfillResult> Handle(
        BackfillTrendyolProductImages request,
        CancellationToken cancellationToken)
        => store.BackfillAsync(cancellationToken);
}

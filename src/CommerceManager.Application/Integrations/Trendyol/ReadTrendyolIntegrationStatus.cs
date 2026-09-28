using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolIntegrationStatus(
    bool IsConnected,
    DateTimeOffset? LastSuccessfulSyncAt,
    DateTimeOffset? LastFailedSyncAt,
    string? LastError,
    DateTimeOffset? LastOrderSyncAt,
    DateTimeOffset? LastReturnSyncAt,
    DateTimeOffset? LastProductSyncAt,
    DateTimeOffset? LastInventorySyncAt,
    DateTimeOffset? UpdatedAt);

public interface ITrendyolIntegrationStatusReader
{
    Task<TrendyolIntegrationStatus?> GetAsync(
        CancellationToken cancellationToken);
}

public sealed record GetTrendyolIntegrationStatus
    : IRequest<TrendyolIntegrationStatus?>;

public sealed class GetTrendyolIntegrationStatusHandler(
    ITrendyolIntegrationStatusReader reader)
    : IRequestHandler<
        GetTrendyolIntegrationStatus,
        TrendyolIntegrationStatus?>
{
    public Task<TrendyolIntegrationStatus?> Handle(
        GetTrendyolIntegrationStatus request,
        CancellationToken cancellationToken)
        => reader.GetAsync(cancellationToken);
}

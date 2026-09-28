using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolOrderPreview(
    int TotalElements,
    int TotalPages,
    int Page,
    int ReceivedPackages);

public interface ITrendyolOrderReader
{
    Task<TrendyolOrderPreview> GetPreviewAsync(CancellationToken cancellationToken);
}

public sealed record GetTrendyolOrderPreview
    : IRequest<TrendyolOrderPreview>;

public sealed class GetTrendyolOrderPreviewHandler(
    ITrendyolOrderReader reader)
    : IRequestHandler<GetTrendyolOrderPreview, TrendyolOrderPreview>
{
    public Task<TrendyolOrderPreview> Handle(
        GetTrendyolOrderPreview request,
        CancellationToken cancellationToken)
        => reader.GetPreviewAsync(cancellationToken);
}

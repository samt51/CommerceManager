using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolReturnItemSnapshot(
    string ClaimItemId,
    string OrderLineId,
    string OrderLineItemId,
    string? Barcode,
    string ProductName,
    string Status);

public sealed record TrendyolReturnSnapshot(
    string ClaimId,
    string OrderNumber,
    string? OrderShipmentPackageId,
    DateTimeOffset ClaimDate,
    DateTimeOffset LastModifiedAt,
    IReadOnlyList<TrendyolReturnItemSnapshot> Items);

public sealed record TrendyolReturnPage(
    IReadOnlyList<TrendyolReturnSnapshot> Returns,
    int TotalPages,
    int TotalElements);

public interface ITrendyolReturnReader
{
    Task<TrendyolReturnPage> ReadPageAsync(
        int page,
        int size,
        long startMs,
        long endMs,
        CancellationToken cancellationToken);
}

public sealed record ReadTrendyolReturns(
    int Days = 30,
    int Page = 0,
    int Size = 50)
    : IRequest<TrendyolReturnPage>;

public sealed class ReadTrendyolReturnsHandler(
    ITrendyolReturnReader reader)
    : IRequestHandler<ReadTrendyolReturns, TrendyolReturnPage>
{
    public Task<TrendyolReturnPage> Handle(
        ReadTrendyolReturns request,
        CancellationToken cancellationToken)
    {
        if (request.Days is < 1 or > 90)
            throw new ArgumentOutOfRangeException(nameof(request.Days));

        if (request.Page < 0)
            throw new ArgumentOutOfRangeException(nameof(request.Page));

        if (request.Size is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(request.Size));

        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-request.Days);

        return reader.ReadPageAsync(
            request.Page,
            request.Size,
            start.ToUnixTimeMilliseconds(),
            end.ToUnixTimeMilliseconds(),
            cancellationToken);
    }
}

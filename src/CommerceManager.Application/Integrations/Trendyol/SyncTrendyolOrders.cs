using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolLineSnapshot(
    string LineId, string? Barcode, string ProductName, int Quantity, decimal UnitPrice);
public sealed record TrendyolPackageSnapshot(
    string PackageId, string OrderNumber, string Status,
    DateTimeOffset OrderedAt, DateTimeOffset LastModifiedAt,
    decimal TotalAmount, IReadOnlyList<TrendyolLineSnapshot> Lines);
public sealed record TrendyolPackagePage(IReadOnlyList<TrendyolPackageSnapshot> Packages, int TotalPages);

public interface ITrendyolPackageReader
{
    Task<TrendyolPackagePage> ReadPageAsync(int page, int size, long startMs, long endMs, CancellationToken cancellationToken);
}
public interface ITrendyolPackageStore
{
    Task<bool> UpsertAsync(TrendyolPackageSnapshot package, CancellationToken cancellationToken);
}
public sealed record SyncTrendyolOrders(int Size = 50, int MaxPages = 20) : IRequest<SyncTrendyolOrdersResult>;
public sealed record SyncTrendyolOrdersResult(int Received, int Inserted, int Updated);

public sealed class SyncTrendyolOrdersHandler(
    ITrendyolPackageReader reader, ITrendyolPackageStore store)
    : IRequestHandler<SyncTrendyolOrders, SyncTrendyolOrdersResult>
{
    public async Task<SyncTrendyolOrdersResult> Handle(
        SyncTrendyolOrders request, CancellationToken cancellationToken)
    {
        if (request.Size is < 1 or > 200 || request.MaxPages is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(request));
        var endMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var startMs = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var received = 0;
        var inserted = 0;
        var updated = 0;
        for (var index = 0; index < request.MaxPages; index++)
        {
            var page = await reader.ReadPageAsync(index, request.Size,
                startMs, endMs, cancellationToken);
            if (page.Packages.Count == 0) break;
            foreach (var package in page.Packages)
            {
                if (!seen.Add(package.PackageId)) continue;
                received++;
                if (await store.UpsertAsync(package, cancellationToken)) inserted++;
                else updated++;
            }
            if (index + 1 >= page.TotalPages) break;
        }
        return new SyncTrendyolOrdersResult(received, inserted, updated);
    }
}

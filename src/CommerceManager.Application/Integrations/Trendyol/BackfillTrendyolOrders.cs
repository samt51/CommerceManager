using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public sealed record TrendyolStreamPage(
    IReadOnlyList<TrendyolPackageSnapshot> Packages, bool HasMore, string? NextCursor);
public interface ITrendyolStreamReader
{
    Task<TrendyolStreamPage> ReadAsync(
        long startMs, long endMs, string? cursor, CancellationToken cancellationToken);
}
public sealed record BackfillTrendyolOrders(int Days = 14) : IRequest<SyncTrendyolOrdersResult>;
public sealed class BackfillTrendyolOrdersHandler(
    ITrendyolStreamReader reader, ITrendyolPackageStore store)
    : IRequestHandler<BackfillTrendyolOrders, SyncTrendyolOrdersResult>
{
    public async Task<SyncTrendyolOrdersResult> Handle(
        BackfillTrendyolOrders request, CancellationToken cancellationToken)
    {
        if (request.Days is < 1 or > 90) throw new ArgumentOutOfRangeException(nameof(request.Days));
        var end = DateTimeOffset.UtcNow;
        var earliest = end.AddDays(-request.Days);
        var received = 0;
        var inserted = 0;
        var updated = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (end > earliest)
        {
            var start = end.AddDays(-13);
            if (start < earliest) start = earliest;
            string? cursor = null;
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            for (var pageNumber = 0; ; pageNumber++)
            {
                if (pageNumber >= 100) throw new InvalidOperationException("Stream exceeded 100 pages in one window.");
                if (pageNumber > 0) await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                var page = await reader.ReadAsync(start.ToUnixTimeMilliseconds(),
                    end.ToUnixTimeMilliseconds(), cursor, cancellationToken);
                foreach (var package in page.Packages)
                {
                    if (!seen.Add(package.PackageId)) continue;
                    received++;
                    if (await store.UpsertAsync(package, cancellationToken)) inserted++;
                    else updated++;
                }
                if (!page.HasMore) break;
                if (string.IsNullOrWhiteSpace(page.NextCursor) || !cursors.Add(page.NextCursor))
                    throw new InvalidOperationException("Trendyol returned a missing or repeated cursor.");
                cursor = page.NextCursor;
            }
            end = start;
        }
        return new SyncTrendyolOrdersResult(received, inserted, updated);
    }
}

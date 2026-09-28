using MediatR;

namespace CommerceManager.Application.Integrations.Trendyol;

public interface ITrendyolReturnStore
{
    Task<TrendyolReturnStoreResult> UpsertAsync(
        TrendyolReturnSnapshot claim,
        TrendyolReturnItemSnapshot item,
        CancellationToken cancellationToken);
}

public sealed record TrendyolReturnStoreResult(
    bool Inserted,
    bool StockApplied,
    bool OrderNotFound,
    bool OrderItemNotFound);

public sealed record SyncTrendyolReturns(
    int Days = 30,
    int Size = 50)
    : IRequest<SyncTrendyolReturnsResult>;

public sealed record SyncTrendyolReturnsResult(
    int Received,
    int Inserted,
    int Updated,
    int StockApplied,
    int OrderNotFound,
    int OrderItemNotFound);

public sealed class SyncTrendyolReturnsHandler(
    ITrendyolReturnReader reader,
    ITrendyolReturnStore store)
    : IRequestHandler<SyncTrendyolReturns, SyncTrendyolReturnsResult>
{
    public async Task<SyncTrendyolReturnsResult> Handle(
        SyncTrendyolReturns request,
        CancellationToken cancellationToken)
    {
        if (request.Days is < 1 or > 90)
            throw new ArgumentOutOfRangeException(nameof(request.Days));

        if (request.Size is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(request.Size));

        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-request.Days);

        var received = 0;
        var inserted = 0;
        var updated = 0;
        var stockApplied = 0;
        var orderNotFound = 0;
        var orderItemNotFound = 0;

        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var pageNumber = 0; ; pageNumber++)
        {
            if (pageNumber >= 100)
                throw new InvalidOperationException(
                    "Trendyol returns exceeded 100 pages.");

            var page = await reader.ReadPageAsync(
                pageNumber,
                request.Size,
                start.ToUnixTimeMilliseconds(),
                end.ToUnixTimeMilliseconds(),
                cancellationToken);

            foreach (var claim in page.Returns)
            {
                foreach (var item in claim.Items)
                {
                    if (!seen.Add(item.ClaimItemId))
                        continue;

                    received++;

                    var result = await store.UpsertAsync(
                        claim,
                        item,
                        cancellationToken);

                    if (result.OrderNotFound)
                    {
                        orderNotFound++;
                        continue;
                    }

                    if (result.OrderItemNotFound)
                    {
                        orderItemNotFound++;
                        continue;
                    }

                    if (result.Inserted)
                        inserted++;
                    else
                        updated++;

                    if (result.StockApplied)
                        stockApplied++;
                }
            }

            if (pageNumber + 1 >= page.TotalPages)
                break;

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        return new SyncTrendyolReturnsResult(
            received,
            inserted,
            updated,
            stockApplied,
            orderNotFound,
            orderItemNotFound);
    }
}

using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Integrations;
using MediatR;

namespace CommerceManager.Api.Integrations.Trendyol;

public sealed class TrendyolOrderSyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TrendyolOrderSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>(
                "Trendyol:BackgroundSyncEnabled"))
        {
            logger.LogInformation(
                "Trendyol automatic order sync is disabled.");

            return;
        }

        var minutes =
            configuration.GetValue<int?>(
                "Trendyol:SyncIntervalMinutes") ?? 5;

        if (minutes is < 5 or > 1440)
        {
            throw new InvalidOperationException(
                "Trendyol:SyncIntervalMinutes must be between 5 and 1440.");
        }

        using var timer =
            new PeriodicTimer(
                TimeSpan.FromMinutes(minutes));

        do
        {
            using var scope =
                scopeFactory.CreateScope();

            var statusStore =
                scope.ServiceProvider
                    .GetRequiredService<
                        MarketplaceIntegrationStatusStore>();

            try
            {
                var sender =
                    scope.ServiceProvider
                        .GetRequiredService<ISender>();

                var result =
                    await sender.Send(
                        new BackfillTrendyolOrders(1),
                        stoppingToken);

                await statusStore
                    .MarkOrderSyncSucceededAsync(
                        SalesChannel.Trendyol,
                        stoppingToken);

                logger.LogInformation(
                    "Trendyol orders synced: received={Received}, inserted={Inserted}, updated={Updated}",
                    result.Received,
                    result.Inserted,
                    result.Updated);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Trendyol automatic order sync failed; will retry at the next interval.");

                try
                {
                    await statusStore.MarkFailedAsync(
                        SalesChannel.Trendyol,
                        exception.Message,
                        stoppingToken);
                }
                catch (Exception statusException)
                {
                    logger.LogError(
                        statusException,
                        "Trendyol integration status could not be updated.");
                }
            }
        }
        while (await timer.WaitForNextTickAsync(
            stoppingToken));
    }
}

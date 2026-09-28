using CommerceManager.Application.Integrations.Trendyol;
using CommerceManager.Domain.Orders;
using CommerceManager.Infrastructure.Integrations;
using MediatR;

namespace CommerceManager.Api.Integrations.Trendyol;

public sealed class TrendyolReturnSyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TrendyolReturnSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var enabled =
            configuration.GetValue(
                "Trendyol:ReturnBackgroundSyncEnabled",
                true);

        if (!enabled)
        {
            logger.LogInformation(
                "Trendyol return background sync is disabled.");

            return;
        }

        var intervalMinutes =
            configuration.GetValue(
                "Trendyol:ReturnSyncIntervalMinutes",
                15);

        intervalMinutes =
            Math.Clamp(intervalMinutes, 5, 1440);

        var syncDays =
            configuration.GetValue(
                "Trendyol:ReturnSyncDays",
                7);

        syncDays =
            Math.Clamp(syncDays, 1, 30);

        logger.LogInformation(
            "Trendyol return background sync started. Interval={IntervalMinutes} minutes, Days={SyncDays}",
            intervalMinutes,
            syncDays);

        while (!stoppingToken.IsCancellationRequested)
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
                        new SyncTrendyolReturns(
                            Days: syncDays,
                            Size: 50),
                        stoppingToken);

                await statusStore
                    .MarkReturnSyncSucceededAsync(
                        SalesChannel.Trendyol,
                        stoppingToken);

                logger.LogInformation(
                    "Trendyol returns synced: received={Received}, inserted={Inserted}, updated={Updated}, stockApplied={StockApplied}, orderNotFound={OrderNotFound}, orderItemNotFound={OrderItemNotFound}",
                    result.Received,
                    result.Inserted,
                    result.Updated,
                    result.StockApplied,
                    result.OrderNotFound,
                    result.OrderItemNotFound);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Trendyol return background sync failed.");

                try
                {
                    await statusStore.MarkFailedAsync(
                        SalesChannel.Trendyol,
                        ex.Message,
                        stoppingToken);
                }
                catch (Exception statusException)
                {
                    logger.LogError(
                        statusException,
                        "Trendyol integration status could not be updated.");
                }
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(
                        intervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

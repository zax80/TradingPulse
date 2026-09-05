using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPulse.Application.Abstractions;

namespace TradingPulse.Infrastructure.Pricing;

/// <summary>
/// Periodically flushes the in-memory latest-price state (<see cref="IPriceStateRepository"/>)
/// to durable storage (<see cref="IPriceSnapshotStore"/>). Deliberately not on
/// every tick: at 10 symbols ticking every 200-600ms that would be 15-50
/// writes/sec for state whose only purpose is surviving a restart, not
/// serving reads - reads are always served from the in-memory store.
/// </summary>
public sealed class PriceStatePersistenceService(
    IPriceStateRepository priceStateRepository,
    IPriceSnapshotStore priceSnapshotStore,
    ILogger<PriceStatePersistenceService> logger) : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snapshots = await priceStateRepository.GetAllLatestAsync(stoppingToken);
                if (snapshots.Count > 0)
                {
                    await priceSnapshotStore.SaveLatestAsync(snapshots, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to persist latest price snapshots");
            }
        }
    }
}

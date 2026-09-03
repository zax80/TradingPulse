using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPulse.Application.Abstractions;

namespace TradingPulse.Infrastructure.Pricing;

/// <summary>
/// Single consumer that drains the pricing engine's merged tick stream and
/// applies each tick to <see cref="IPriceStateRepository"/>. Processing
/// ticks one at a time, regardless of symbol, is what makes the per-symbol
/// price-state update safe without any extra locking here.
/// </summary>
public sealed class PriceTickProcessor(
    IPricingEngine pricingEngine,
    IPriceStateRepository priceStateRepository,
    ILogger<PriceTickProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var tick in pricingEngine.StreamAsync(stoppingToken))
        {
            try
            {
                await priceStateRepository.ApplyTickAsync(tick, stoppingToken);

                // TODO (Day 3): evaluate spread-based auto-trading here, using the returned snapshot.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process tick for {Symbol}", tick.Symbol);
            }
        }
    }
}

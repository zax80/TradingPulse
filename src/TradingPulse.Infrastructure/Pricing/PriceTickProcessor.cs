using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Infrastructure.Pricing;

/// <summary>
/// Single consumer that drains the pricing engine's merged tick stream,
/// applies each tick to <see cref="IPriceStateRepository"/>, and evaluates
/// spread-based auto-trading on the resulting snapshot. Processing ticks
/// one at a time, regardless of symbol, is what makes the per-symbol
/// price-state update safe without any extra locking here.
/// </summary>
public sealed class PriceTickProcessor(
    IPricingEngine pricingEngine,
    IPriceStateRepository priceStateRepository,
    IAutoTradingService autoTradingService,
    ITradingRulesRepository tradingRulesRepository,
    IOrderSubmissionService orderSubmissionService,
    ILogger<PriceTickProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var tick in pricingEngine.StreamAsync(stoppingToken))
        {
            try
            {
                var latest = await priceStateRepository.ApplyTickAsync(tick, stoppingToken);
                await EvaluateAutoTradingAsync(latest, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process tick for {Symbol}", tick.Symbol);
            }
        }
    }

    private async Task EvaluateAutoTradingAsync(PriceSnapshot latest, CancellationToken cancellationToken)
    {
        var rules = await tradingRulesRepository.GetCurrentAsync(cancellationToken);

        var candidate = autoTradingService.TryCreateOrder(latest, rules.AutoTradingSpreadPercentThreshold);
        if (candidate is null)
        {
            return;
        }

        // Same submission pipeline as the API - get-price/check-duplicate/evaluate/persist lives in one place.
        var decision = await orderSubmissionService.SubmitAsync(candidate, cancellationToken);

        if (decision.Status == DecisionStatus.Rejected)
        {
            logger.LogInformation(
                "Auto-generated {Side} order for {Symbol} rejected: {Reasons}",
                candidate.Side, candidate.Symbol, string.Join("; ", decision.RejectionReasons));
        }
    }
}

using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Builds the candidate auto-generated order for a price tick. Only
/// decides whether an order is warranted and what it looks like; the
/// caller still validates it through <see cref="ITradingRulesEngine"/>.
/// </summary>
public interface IAutoTradingService
{
    /// <param name="latest">
    /// The symbol's latest price snapshot. <see cref="PriceSnapshot.PreviousMarketPrice"/>
    /// already carries the prior tick's mid price, so no separate "previous" snapshot is needed.
    /// </param>
    /// <param name="spreadPercentThreshold">Configured spread-percent threshold.</param>
    /// <returns>The auto-generated order, or <c>null</c> if none should be created this tick.</returns>
    Order? TryCreateOrder(PriceSnapshot latest, decimal spreadPercentThreshold);
}

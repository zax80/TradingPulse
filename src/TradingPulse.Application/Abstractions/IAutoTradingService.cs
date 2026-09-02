using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Builds the candidate auto-generated order for a price tick. Only
/// decides whether an order is warranted and what it looks like; the
/// caller still validates it through <see cref="ITradingRulesEngine"/>.
/// </summary>
public interface IAutoTradingService
{
    /// <param name="previous">The symbol's price snapshot before this tick.</param>
    /// <param name="latest">The symbol's price snapshot after this tick.</param>
    /// <param name="spreadPercentThreshold">Configured spread-percent threshold.</param>
    /// <returns>The auto-generated order, or <c>null</c> if none should be created this tick.</returns>
    Order? TryCreateOrder(PriceSnapshot previous, PriceSnapshot latest, decimal spreadPercentThreshold);
}

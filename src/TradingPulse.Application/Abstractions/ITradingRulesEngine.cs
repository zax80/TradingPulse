using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Evaluates one order against the trading rules. Synchronous and
/// side-effect-free — all required data is passed in, all I/O stays with the caller.
/// </summary>
public interface ITradingRulesEngine
{
    /// <param name="order">The order to validate.</param>
    /// <param name="rules">The trading rules snapshot currently in effect.</param>
    /// <param name="currentPrice">The symbol's latest price snapshot, if any.</param>
    /// <param name="isDuplicateClientOrderId">Whether <see cref="Order.ClientOrderId"/> has already been seen.</param>
    OrderDecision Evaluate(Order order, TradingRules rules, PriceSnapshot? currentPrice, bool isDuplicateClientOrderId);
}

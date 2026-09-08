using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Infrastructure.Rules;

/// <summary>Pure, synchronous trading-rules evaluation. See <see cref="ITradingRulesEngine"/>.</summary>
public sealed class TradingRulesEngine : ITradingRulesEngine
{
    public OrderDecision Evaluate(Order order, TradingRules rules, PriceSnapshot? currentPrice, bool isDuplicateClientOrderId)
    {
        List<RejectionReason> reasons = [];

        AddIfViolated(reasons, CheckMaxNotional(order, rules));
        AddIfViolated(reasons, CheckMaxQuantity(order, rules));
        AddIfViolated(reasons, CheckPriceDeviation(order, rules, currentPrice));
        AddIfViolated(reasons, CheckDuplicateOrderId(order, rules, isDuplicateClientOrderId));
        AddIfViolated(reasons, CheckSymbolWhitelist(order, rules));

        var decidedAt = DateTimeOffset.UtcNow;
        return reasons.Count == 0
            ? OrderDecision.Accept(order.Id, decidedAt)
            : OrderDecision.Reject(order.Id, reasons, decidedAt);
    }

    private static void AddIfViolated(List<RejectionReason> reasons, RejectionReason? reason)
    {
        if (reason is not null)
        {
            reasons.Add(reason);
        }
    }

    private static RejectionReason? CheckMaxNotional(Order order, TradingRules rules) =>
        order.Notional > rules.MaxNotionalPerOrder
            ? new RejectionReason(
                RejectionReasonCode.MaxNotionalExceeded,
                $"Notional {order.Notional} exceeds the maximum of {rules.MaxNotionalPerOrder}.")
            : null;

    private static RejectionReason? CheckMaxQuantity(Order order, TradingRules rules) =>
        order.Quantity > rules.MaxQuantityPerOrder
            ? new RejectionReason(
                RejectionReasonCode.MaxQuantityExceeded,
                $"Quantity {order.Quantity} exceeds the maximum of {rules.MaxQuantityPerOrder}.")
            : null;

    private static RejectionReason? CheckPriceDeviation(Order order, TradingRules rules, PriceSnapshot? currentPrice)
    {
        if (currentPrice is not PriceSnapshot price)
        {
            return new RejectionReason(
                RejectionReasonCode.NoCurrentPrice,
                $"No current market price available for {order.Symbol}; cannot validate price deviation.");
        }

        var deviationPercent = Math.Abs(order.Price - price.CurrentMarketPrice) / price.CurrentMarketPrice * 100m;
        return deviationPercent > rules.PriceDeviationThresholdPercent
            ? new RejectionReason(
                RejectionReasonCode.PriceDeviationExceeded,
                $"Price {order.Price} deviates {deviationPercent:F4}% from mid {price.CurrentMarketPrice}, exceeding the {rules.PriceDeviationThresholdPercent}% threshold.")
            : null;
    }

    private static RejectionReason? CheckDuplicateOrderId(Order order, TradingRules rules, bool isDuplicateClientOrderId) =>
        rules.DuplicateOrderIdCheckEnabled && isDuplicateClientOrderId
            ? new RejectionReason(
                RejectionReasonCode.DuplicateClientOrderId,
                $"Client order id '{order.ClientOrderId}' has already been used with different order details.")
            : null;

    private static RejectionReason? CheckSymbolWhitelist(Order order, TradingRules rules) =>
        rules.SymbolWhitelistEnabled && !rules.SymbolWhitelist.Contains(order.Symbol, StringComparer.OrdinalIgnoreCase)
            ? new RejectionReason(
                RejectionReasonCode.SymbolNotWhitelisted,
                $"Symbol '{order.Symbol}' is not on the whitelist.")
            : null;
}

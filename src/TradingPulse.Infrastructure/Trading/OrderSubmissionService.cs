using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Trading;

/// <summary>Orchestrates the get-price / check-duplicate / evaluate / persist pipeline. See <see cref="IOrderSubmissionService"/>.</summary>
public sealed class OrderSubmissionService(
    IPriceStateRepository priceStateRepository,
    ITradingRulesRepository tradingRulesRepository,
    ITradingRulesEngine tradingRulesEngine,
    IOrderRepository orderRepository) : IOrderSubmissionService
{
    public async Task<OrderDecision> SubmitAsync(Order order, CancellationToken cancellationToken = default)
    {
        var rules = await tradingRulesRepository.GetCurrentAsync(cancellationToken);
        var existing = await orderRepository.GetByClientOrderIdAsync(order.ClientOrderId, cancellationToken);

        if (existing is not null && rules.DuplicateOrderIdCheckEnabled && IsSameRequest(existing.Order, order))
        {
            // Idempotent replay: an identical retry of a request already decided (e.g. the client
            // never saw the original response and resubmitted) returns the original decision
            // instead of evaluating - and persisting - a new one. A reused ClientOrderId with
            // *different* order details is not a retry; it falls through to Evaluate below, where
            // the duplicate-id rule rejects it as before.
            return existing.Decision;
        }

        var currentPrice = await priceStateRepository.GetLatestAsync(order.Symbol, cancellationToken);
        var isDuplicate = existing is not null;
        var decision = tradingRulesEngine.Evaluate(order, rules, currentPrice, isDuplicate);

        await orderRepository.AddAsync(order, decision, cancellationToken);

        return decision;
    }

    private static bool IsSameRequest(Order existing, Order incoming) =>
        string.Equals(existing.Symbol, incoming.Symbol, StringComparison.OrdinalIgnoreCase)
        && existing.Side == incoming.Side
        && existing.Type == incoming.Type
        && existing.Price == incoming.Price
        && existing.Quantity == incoming.Quantity;
}

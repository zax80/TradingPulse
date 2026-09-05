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
        var currentPrice = await priceStateRepository.GetLatestAsync(order.Symbol, cancellationToken);
        var isDuplicate = await orderRepository.ExistsWithClientOrderIdAsync(order.ClientOrderId, cancellationToken);

        var decision = tradingRulesEngine.Evaluate(order, rules, currentPrice, isDuplicate);

        await orderRepository.AddAsync(order, decision, cancellationToken);

        return decision;
    }
}

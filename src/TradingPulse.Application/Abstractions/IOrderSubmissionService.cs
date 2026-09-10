using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Shared submission pipeline for an already-constructed <see cref="Order"/>:
/// look up the current price, check for a duplicate client order id,
/// evaluate the trading rules, and persist the decision. Used identically by
/// the API's submit-order endpoint and by auto-trading, per spec.
/// </summary>
public interface IOrderSubmissionService
{
    Task<OrderDecision> SubmitAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same pipeline as <see cref="SubmitAsync(Order, CancellationToken)"/>, but for a caller that
    /// has already fetched the current <see cref="TradingRules"/> for another reason (e.g.
    /// auto-trading, which needs them to evaluate the spread threshold before it even builds a
    /// candidate order) - avoids a second, redundant <c>ITradingRulesRepository.GetCurrentAsync</c>
    /// call for the same tick/request.
    /// </summary>
    Task<OrderDecision> SubmitAsync(Order order, TradingRules rules, CancellationToken cancellationToken = default);
}

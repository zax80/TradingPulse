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
}

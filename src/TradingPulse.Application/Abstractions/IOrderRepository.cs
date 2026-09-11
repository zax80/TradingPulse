using TradingPulse.Application.Models;
using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Persists orders and their decisions, and backs the order-related API endpoints.</summary>
public interface IOrderRepository
{
    /// <summary>Persists an order together with the decision made for it.</summary>
    Task AddAsync(Order order, OrderDecision decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recent order+decision persisted under this client order id, or <c>null</c> if none.
    /// Backs both the duplicate-id rule and idempotent-retry handling - a single lookup answers
    /// "have we seen this id" and, if so, "what did we decide".
    /// </summary>
    Task<OrderHistoryEntry?> GetByClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The order+decision for a specific <see cref="Order.Id"/>, or <c>null</c> if not found. Used to
    /// resolve a losing <see cref="IClientOrderIdReservationStore"/> race unambiguously - unlike
    /// <see cref="GetByClientOrderIdAsync"/>, which can match more than one row for a ClientOrderId
    /// once a conflicting reuse has been persisted, an id lookup is always exact.
    /// </summary>
    Task<OrderHistoryEntry?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Trade history with basic filtering.</summary>
    Task<IReadOnlyList<OrderHistoryEntry>> GetHistoryAsync(OrderHistoryFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Order history for one symbol.</summary>
    Task<IReadOnlyList<OrderHistoryEntry>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}

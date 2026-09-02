using TradingPulse.Application.Models;
using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Persists orders and their decisions, and backs the order-related API endpoints.</summary>
public interface IOrderRepository
{
    /// <summary>Persists an order together with the decision made for it.</summary>
    Task AddAsync(Order order, OrderDecision decision, CancellationToken cancellationToken = default);

    /// <summary>Whether an order with this client order id has already been persisted. Backs the duplicate-id rule.</summary>
    Task<bool> ExistsWithClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default);

    /// <summary>Trade history with basic filtering.</summary>
    Task<IReadOnlyList<OrderHistoryEntry>> GetHistoryAsync(OrderHistoryFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Order history for one symbol.</summary>
    Task<IReadOnlyList<OrderHistoryEntry>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}

using System.Collections.Concurrent;
using TradingPulse.Application.Abstractions;
using TradingPulse.Application.Models;
using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Orders;

/// <summary>
/// In-memory order + decision store. Temporary - superseded by an EF Core
/// implementation on Day 4; exists now so the trading-rules and
/// auto-trading flow can be exercised end to end before persistence lands.
/// </summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentQueue<OrderHistoryEntry> _entries = new();
    private readonly ConcurrentDictionary<string, byte> _clientOrderIds = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(Order order, OrderDecision decision, CancellationToken cancellationToken = default)
    {
        _entries.Enqueue(new OrderHistoryEntry(order, decision));
        _clientOrderIds.TryAdd(order.ClientOrderId, 0);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_clientOrderIds.ContainsKey(clientOrderId));

    public Task<IReadOnlyList<OrderHistoryEntry>> GetHistoryAsync(OrderHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        IEnumerable<OrderHistoryEntry> query = _entries;

        if (filter.Symbol is not null)
        {
            query = query.Where(e => string.Equals(e.Order.Symbol, filter.Symbol, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Side is not null)
        {
            query = query.Where(e => e.Order.Side == filter.Side);
        }

        if (filter.Origin is not null)
        {
            query = query.Where(e => e.Order.Origin == filter.Origin);
        }

        if (filter.Status is not null)
        {
            query = query.Where(e => e.Decision.Status == filter.Status);
        }

        if (filter.From is not null)
        {
            query = query.Where(e => e.Order.SubmittedAt >= filter.From);
        }

        if (filter.To is not null)
        {
            query = query.Where(e => e.Order.SubmittedAt <= filter.To);
        }

        IReadOnlyList<OrderHistoryEntry> page = query
            .OrderByDescending(e => e.Order.SubmittedAt)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToList();

        return Task.FromResult(page);
    }

    public Task<IReadOnlyList<OrderHistoryEntry>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrderHistoryEntry> results = _entries
            .Where(e => string.Equals(e.Order.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Order.SubmittedAt)
            .ToList();

        return Task.FromResult(results);
    }
}

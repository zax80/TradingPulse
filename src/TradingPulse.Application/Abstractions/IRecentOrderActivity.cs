using TradingPulse.Application.Models;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// A small, bounded, in-memory record of the most recently decided orders - separate from
/// <see cref="IOrderRepository"/>'s durable history. Exists so a UI that just wants "the last
/// N orders, refreshed every couple of seconds" (the Blazor dashboard) doesn't have to issue a
/// database query on every poll, once per open browser tab, to get an answer that's already
/// sitting in process memory from the moment the order was decided. <see cref="IOrderRepository"/>
/// (with its filters/paging) remains the source of truth for anything that needs a real query
/// (the REST history endpoints, anything beyond "recently").
/// </summary>
public interface IRecentOrderActivity
{
    /// <summary>Records a just-decided order. Safe to call from multiple concurrent submitters.</summary>
    void Record(OrderHistoryEntry entry);

    /// <summary>The most recent entries, newest first, capped at whatever the implementation retains.</summary>
    IReadOnlyList<OrderHistoryEntry> GetRecent(int count);
}

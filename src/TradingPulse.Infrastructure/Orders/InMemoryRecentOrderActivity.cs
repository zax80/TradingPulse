using System.Collections.Concurrent;
using System.Linq;
using TradingPulse.Application.Abstractions;
using TradingPulse.Application.Models;

namespace TradingPulse.Infrastructure.Orders;

/// <summary>
/// Lock-free, bounded ring of the most recently decided orders. See <see cref="IRecentOrderActivity"/>
/// for why this exists (the dashboard's "Recent Orders" widget) and why it's deliberately separate
/// from <see cref="IOrderRepository"/>'s durable, queryable history - it is not, and is not meant to
/// be, a source of truth: it holds at most <see cref="MaxEntries"/> entries, in process memory only,
/// and is empty again after a restart.
/// </summary>
public sealed class InMemoryRecentOrderActivity : IRecentOrderActivity
{
    private const int MaxEntries = 50;

    private readonly ConcurrentQueue<OrderHistoryEntry> _entries = new();

    public void Record(OrderHistoryEntry entry)
    {
        _entries.Enqueue(entry);

        // Trim under concurrent writers: each dequeue only removes one entry, so a burst of
        // concurrent Record calls each trims at most one excess entry - the queue converges back
        // to MaxEntries rather than overshooting, without needing a lock around enqueue+trim.
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<OrderHistoryEntry> GetRecent(int count) =>
        _entries.Reverse().Take(count).ToList();
}

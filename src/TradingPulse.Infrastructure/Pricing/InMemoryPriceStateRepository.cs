using System.Collections.Concurrent;
using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Pricing;

/// <summary>
/// In-memory latest-price store, keyed by symbol. The dictionary handles
/// concurrent lookups/inserts of new symbols; per-symbol updates are safe by
/// construction (see <see cref="PriceState"/>) since only <see cref="PriceTickProcessor"/> writes.
/// </summary>
public sealed class InMemoryPriceStateRepository : IPriceStateRepository
{
    private readonly ConcurrentDictionary<string, PriceState> _states = new();

    public Task<PriceSnapshot?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var snapshot = _states.TryGetValue(symbol, out var state) ? state.ToSnapshot() : null;
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<PriceSnapshot>> GetAllLatestAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PriceSnapshot> snapshots = _states.Values
            .Select(state => state.ToSnapshot())
            .Where(snapshot => snapshot.HasValue)
            .Select(snapshot => snapshot!.Value)
            .ToList();

        return Task.FromResult(snapshots);
    }

    public Task<PriceSnapshot> ApplyTickAsync(PriceUpdate tick, CancellationToken cancellationToken = default)
    {
        var state = _states.GetOrAdd(tick.Symbol, PriceState.CreateEmpty);
        return Task.FromResult(state.Apply(tick));
    }
}

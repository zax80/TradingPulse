using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Rules;

/// <summary>
/// In-memory trading-rules store. Temporary - superseded by an EF Core
/// implementation on Day 4. Holds the current rules as an immutable
/// reference, swapped as a whole via <see cref="Volatile"/> on
/// <see cref="SaveAsync"/> (same lock-free pattern as <c>PriceState</c>),
/// so reads never block on a concurrent update.
/// </summary>
public sealed class InMemoryTradingRulesRepository : ITradingRulesRepository
{
    private TradingRules _current = TradingRules.Default;

    public Task<TradingRules> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Volatile.Read(ref _current));

    public Task SaveAsync(TradingRules rules, CancellationToken cancellationToken = default)
    {
        Volatile.Write(ref _current, rules);
        return Task.CompletedTask;
    }
}

using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Persists the active <see cref="TradingRules"/> snapshot and backs the get/update-rules endpoints.</summary>
public interface ITradingRulesRepository
{
    /// <summary>Gets the currently active trading rules.</summary>
    Task<TradingRules> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the active trading rules with a new snapshot.</summary>
    Task SaveAsync(TradingRules rules, CancellationToken cancellationToken = default);
}

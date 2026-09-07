using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Durable copy of the latest price per symbol. Separate from
/// <see cref="IPriceStateRepository"/>, which is the in-memory store the hot
/// tick path reads/writes - this is written periodically, not per tick.
/// </summary>
public interface IPriceSnapshotStore
{
    /// <summary>Inserts or updates the latest snapshot for each symbol given.</summary>
    Task SaveLatestAsync(IReadOnlyList<PriceSnapshot> snapshots, CancellationToken cancellationToken = default);
}

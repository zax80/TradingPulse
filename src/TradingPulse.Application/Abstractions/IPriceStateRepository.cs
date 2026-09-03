using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Owns the latest known price per symbol.</summary>
public interface IPriceStateRepository
{
    /// <summary>Gets the latest snapshot for a symbol, or <c>null</c> if none has been recorded yet.</summary>
    Task<PriceSnapshot?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Gets the latest snapshot for every symbol currently known.</summary>
    Task<IReadOnlyList<PriceSnapshot>> GetAllLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies a tick to the symbol's price state, computing derived values, and returns the resulting snapshot.</summary>
    Task<PriceSnapshot> ApplyTickAsync(PriceUpdate tick, CancellationToken cancellationToken = default);
}

using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Owns the latest known price per symbol.</summary>
public interface IPriceStateRepository
{
    /// <summary>Gets the latest snapshot for a symbol, or <c>null</c> if none has been recorded yet.</summary>
    Task<PriceSnapshot?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Gets the latest snapshot for every symbol currently known.</summary>
    Task<IReadOnlyList<PriceSnapshot>> GetAllLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a symbol's new latest snapshot, replacing whatever was there before.</summary>
    Task UpsertAsync(PriceSnapshot snapshot, CancellationToken cancellationToken = default);
}

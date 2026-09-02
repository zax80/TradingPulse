using TradingPulse.Domain;

namespace TradingPulse.Application.Abstractions;

/// <summary>Simulates the market data feed: continuously generates price ticks for multiple symbols.</summary>
public interface IPricingEngine
{
    /// <summary>Streams price ticks until <paramref name="cancellationToken"/> is cancelled.</summary>
    IAsyncEnumerable<PriceUpdate> StreamAsync(CancellationToken cancellationToken);
}

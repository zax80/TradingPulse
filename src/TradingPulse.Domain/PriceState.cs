namespace TradingPulse.Domain;

/// <summary>
/// Mutable "latest known state" cell for one symbol. Holds an immutable
/// <see cref="PriceSnapshot"/> that gets atomically replaced on each tick
/// via <see cref="Volatile"/> read/write over a private box, so a reader
/// never sees a partially-updated snapshot and no lock is needed. Expected
/// usage is a single writer per symbol; readers use <see cref="ToSnapshot"/>.
/// </summary>
public sealed class PriceState
{
    private sealed class Box
    {
        public readonly PriceSnapshot Snapshot;
        public Box(PriceSnapshot snapshot) => Snapshot = snapshot;
    }

    public string Symbol { get; }

    private Box? _box;

    private PriceState(string symbol)
    {
        Symbol = symbol;
    }

    /// <summary>Creates the empty, "no tick seen yet" state for a symbol.</summary>
    public static PriceState CreateEmpty(string symbol) => new(symbol);

    /// <summary>Applies a new tick, computes the derived values, and returns the resulting snapshot.</summary>
    public PriceSnapshot Apply(PriceUpdate update)
    {
        if (update.Symbol != Symbol)
        {
            throw new InvalidOperationException(
                $"Tick for symbol '{update.Symbol}' cannot be applied to price state for '{Symbol}'.");
        }

        var previousMarketPrice = Volatile.Read(ref _box)?.Snapshot.CurrentMarketPrice;
        var currentMarketPrice = (update.BidPrice + update.AskPrice) / 2m;
        var spread = update.AskPrice - update.BidPrice;
        var spreadPercent = currentMarketPrice == 0 ? 0 : spread / currentMarketPrice * 100m;

        var snapshot = new PriceSnapshot(
            Symbol,
            update.BidPrice,
            update.AskPrice,
            currentMarketPrice,
            spread,
            spreadPercent,
            previousMarketPrice,
            update.Timestamp);

        Volatile.Write(ref _box, new Box(snapshot));
        return snapshot;
    }

    /// <summary>The latest snapshot, or <c>null</c> if no tick has been applied yet. Safe to call concurrently with <see cref="Apply"/>.</summary>
    public PriceSnapshot? ToSnapshot() => Volatile.Read(ref _box)?.Snapshot;
}

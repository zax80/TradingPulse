namespace TradingPulse.Domain;

/// <summary>
/// Mutable "latest known state" aggregate for one symbol. Expected usage is
/// one instance per symbol, updated by a single writer; readers should use
/// <see cref="ToSnapshot"/> rather than reading properties directly.
/// </summary>
public sealed class PriceState
{
    public string Symbol { get; }
    public decimal BidPrice { get; private set; }
    public decimal AskPrice { get; private set; }
    public decimal CurrentMarketPrice { get; private set; }
    public decimal Spread { get; private set; }
    public decimal SpreadPercent { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>The <see cref="CurrentMarketPrice"/> as of the previous tick, or <c>null</c> before the first tick.</summary>
    public decimal? PreviousMarketPrice { get; private set; }

    private PriceState(string symbol)
    {
        Symbol = symbol;
    }

    /// <summary>Creates the empty, "no tick seen yet" state for a symbol.</summary>
    public static PriceState CreateEmpty(string symbol) => new(symbol);

    /// <summary>Applies a new tick and recomputes the derived values.</summary>
    public void Apply(PriceUpdate update)
    {
        if (update.Symbol != Symbol)
        {
            throw new InvalidOperationException(
                $"Tick for symbol '{update.Symbol}' cannot be applied to price state for '{Symbol}'.");
        }

        var newMarketPrice = (update.BidPrice + update.AskPrice) / 2m;

        PreviousMarketPrice = Timestamp == default ? null : CurrentMarketPrice;

        BidPrice = update.BidPrice;
        AskPrice = update.AskPrice;
        CurrentMarketPrice = newMarketPrice;
        Spread = update.AskPrice - update.BidPrice;
        SpreadPercent = newMarketPrice == 0 ? 0 : Spread / newMarketPrice * 100m;
        Timestamp = update.Timestamp;
    }

    public PriceSnapshot ToSnapshot() => new(
        Symbol,
        BidPrice,
        AskPrice,
        CurrentMarketPrice,
        Spread,
        SpreadPercent,
        PreviousMarketPrice,
        Timestamp);
}

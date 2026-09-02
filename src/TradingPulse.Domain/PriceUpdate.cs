namespace TradingPulse.Domain;

/// <summary>
/// A single market data tick for one symbol. Immutable value type
/// (<c>readonly record struct</c>) to avoid a heap allocation per tick on
/// the pricing engine's hot path.
/// </summary>
public readonly record struct PriceUpdate
{
    public string Symbol { get; }
    public decimal BidPrice { get; }
    public decimal AskPrice { get; }
    public DateTimeOffset Timestamp { get; }

    private PriceUpdate(string symbol, decimal bidPrice, decimal askPrice, DateTimeOffset timestamp)
    {
        Symbol = symbol;
        BidPrice = bidPrice;
        AskPrice = askPrice;
        Timestamp = timestamp;
    }

    /// <summary>Creates a <see cref="PriceUpdate"/>, validating that <see cref="BidPrice"/> is positive and less than <see cref="AskPrice"/>.</summary>
    public static bool TryCreate(
        string symbol,
        decimal bidPrice,
        decimal askPrice,
        DateTimeOffset timestamp,
        out PriceUpdate update,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            update = default;
            error = "Symbol is required.";
            return false;
        }

        if (bidPrice <= 0 || askPrice <= 0)
        {
            update = default;
            error = "BidPrice and AskPrice must be positive.";
            return false;
        }

        if (bidPrice >= askPrice)
        {
            update = default;
            error = $"BidPrice ({bidPrice}) must be less than AskPrice ({askPrice}).";
            return false;
        }

        update = new PriceUpdate(symbol, bidPrice, askPrice, timestamp);
        error = null;
        return true;
    }
}

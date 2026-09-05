using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Durable copy of a symbol's latest price snapshot. See <see cref="Pricing.PriceStatePersistenceService"/>.</summary>
public sealed class PriceStateEntity
{
    public required string Symbol { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal CurrentMarketPrice { get; set; }
    public decimal Spread { get; set; }
    public decimal SpreadPercent { get; set; }
    public decimal? PreviousMarketPrice { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public static PriceStateEntity FromDomain(PriceSnapshot snapshot) => new()
    {
        Symbol = snapshot.Symbol,
        BidPrice = snapshot.BidPrice,
        AskPrice = snapshot.AskPrice,
        CurrentMarketPrice = snapshot.CurrentMarketPrice,
        Spread = snapshot.Spread,
        SpreadPercent = snapshot.SpreadPercent,
        PreviousMarketPrice = snapshot.PreviousMarketPrice,
        Timestamp = snapshot.Timestamp,
    };

    public void UpdateFrom(PriceSnapshot snapshot)
    {
        BidPrice = snapshot.BidPrice;
        AskPrice = snapshot.AskPrice;
        CurrentMarketPrice = snapshot.CurrentMarketPrice;
        Spread = snapshot.Spread;
        SpreadPercent = snapshot.SpreadPercent;
        PreviousMarketPrice = snapshot.PreviousMarketPrice;
        Timestamp = snapshot.Timestamp;
    }

    public PriceSnapshot ToDomain() =>
        new(Symbol, BidPrice, AskPrice, CurrentMarketPrice, Spread, SpreadPercent, PreviousMarketPrice, Timestamp);
}

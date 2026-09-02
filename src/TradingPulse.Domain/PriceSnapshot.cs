namespace TradingPulse.Domain;

/// <summary>
/// Immutable, point-in-time view of a symbol's latest price state. Safe to
/// read concurrently, unlike the mutable <see cref="PriceState"/> it's copied from.
/// </summary>
public readonly record struct PriceSnapshot(
    string Symbol,
    decimal BidPrice,
    decimal AskPrice,
    decimal CurrentMarketPrice,
    decimal Spread,
    decimal SpreadPercent,
    decimal? PreviousMarketPrice,
    DateTimeOffset Timestamp);

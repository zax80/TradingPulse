using TradingPulse.Domain.Enums;

namespace TradingPulse.Application.Models;

/// <summary>Optional filters for the trade-history endpoint. A null field means "don't filter on this".</summary>
public sealed record OrderHistoryFilter(
    string? Symbol = null,
    OrderSide? Side = null,
    OrderOrigin? Origin = null,
    DecisionStatus? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Skip = 0,
    int Take = 100);

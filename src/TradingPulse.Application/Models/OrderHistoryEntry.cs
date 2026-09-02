using TradingPulse.Domain;

namespace TradingPulse.Application.Models;

/// <summary>An <see cref="Order"/> joined with its <see cref="OrderDecision"/>.</summary>
public sealed record OrderHistoryEntry(Order Order, OrderDecision Decision);

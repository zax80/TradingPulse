namespace TradingPulse.Domain.Enums;

/// <summary>Final outcome of evaluating an order against the trading rules.</summary>
public enum DecisionStatus
{
    Accepted = 0,
    Rejected = 1,
}

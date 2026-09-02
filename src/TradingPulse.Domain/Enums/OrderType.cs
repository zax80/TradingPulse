namespace TradingPulse.Domain.Enums;

/// <summary>Execution style requested for an order. Only <see cref="Limit"/> is currently supported.</summary>
public enum OrderType
{
    Limit = 0,
}

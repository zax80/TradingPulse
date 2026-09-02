namespace TradingPulse.Domain;

/// <summary>
/// The active set of configurable trading rules. Immutable — "updating" the
/// rules means swapping in a new instance as a whole, so the hot path can
/// read the current rules without locking.
/// </summary>
public sealed record TradingRules
{
    /// <summary>Reject an order if <c>Price × Quantity</c> exceeds this.</summary>
    public required decimal MaxNotionalPerOrder { get; init; }

    /// <summary>Reject an order if its quantity exceeds this.</summary>
    public required decimal MaxQuantityPerOrder { get; init; }

    /// <summary>Reject an order if its price deviates from the mid price by more than this many percent.</summary>
    public decimal PriceDeviationThresholdPercent { get; init; } = 0.8m;

    /// <summary>Toggle for the duplicate-ClientOrderId rejection rule.</summary>
    public bool DuplicateOrderIdCheckEnabled { get; init; } = true;

    /// <summary>Toggle for the symbol-whitelist rule.</summary>
    public bool SymbolWhitelistEnabled { get; init; }

    /// <summary>Symbols allowed to trade when <see cref="SymbolWhitelistEnabled"/> is true.</summary>
    public IReadOnlyCollection<string> SymbolWhitelist { get; init; } = Array.Empty<string>();

    /// <summary>Spread-percent threshold that triggers auto-trading.</summary>
    public required decimal AutoTradingSpreadPercentThreshold { get; init; }

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Reasonable defaults so the service is usable before the rules are configured.</summary>
    public static TradingRules Default => new()
    {
        MaxNotionalPerOrder = 1_000_000m,
        MaxQuantityPerOrder = 10_000m,
        PriceDeviationThresholdPercent = 0.8m,
        DuplicateOrderIdCheckEnabled = true,
        SymbolWhitelistEnabled = false,
        SymbolWhitelist = Array.Empty<string>(),
        AutoTradingSpreadPercentThreshold = 0.5m,
    };
}

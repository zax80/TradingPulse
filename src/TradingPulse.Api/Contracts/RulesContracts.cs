namespace TradingPulse.Api.Contracts;

/// <summary>
/// Request body for <c>PUT /api/rules</c>. A full replace, matching
/// <c>ITradingRulesRepository.SaveAsync</c> - the two required fields must be
/// supplied, everything else falls back to <c>TradingRules.Default</c> when omitted.
/// </summary>
public sealed record UpdateTradingRulesRequest(
    decimal MaxNotionalPerOrder,
    decimal MaxQuantityPerOrder,
    decimal AutoTradingSpreadPercentThreshold,
    decimal? PriceDeviationThresholdPercent = null,
    bool? DuplicateOrderIdCheckEnabled = null,
    bool? SymbolWhitelistEnabled = null,
    IReadOnlyCollection<string>? SymbolWhitelist = null);

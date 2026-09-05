using TradingPulse.Api.Contracts;
using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;

namespace TradingPulse.Api.Endpoints;

public static class RulesEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rules", GetRulesAsync);
        app.MapPut("/api/rules", UpdateRulesAsync);

        return app;
    }

    private static async Task<IResult> GetRulesAsync(ITradingRulesRepository repository, CancellationToken cancellationToken) =>
        Results.Ok(await repository.GetCurrentAsync(cancellationToken));

    private static async Task<IResult> UpdateRulesAsync(
        UpdateTradingRulesRequest request,
        ITradingRulesRepository repository,
        CancellationToken cancellationToken)
    {
        if (request.MaxNotionalPerOrder <= 0 || request.MaxQuantityPerOrder <= 0)
        {
            return Results.BadRequest("MaxNotionalPerOrder and MaxQuantityPerOrder must be positive.");
        }

        if (request.AutoTradingSpreadPercentThreshold <= 0)
        {
            return Results.BadRequest("AutoTradingSpreadPercentThreshold must be positive.");
        }

        var rules = new TradingRules
        {
            MaxNotionalPerOrder = request.MaxNotionalPerOrder,
            MaxQuantityPerOrder = request.MaxQuantityPerOrder,
            AutoTradingSpreadPercentThreshold = request.AutoTradingSpreadPercentThreshold,
            PriceDeviationThresholdPercent = request.PriceDeviationThresholdPercent ?? TradingRules.Default.PriceDeviationThresholdPercent,
            DuplicateOrderIdCheckEnabled = request.DuplicateOrderIdCheckEnabled ?? TradingRules.Default.DuplicateOrderIdCheckEnabled,
            SymbolWhitelistEnabled = request.SymbolWhitelistEnabled ?? TradingRules.Default.SymbolWhitelistEnabled,
            SymbolWhitelist = request.SymbolWhitelist ?? TradingRules.Default.SymbolWhitelist,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await repository.SaveAsync(rules, cancellationToken);
        return Results.Ok(rules);
    }
}

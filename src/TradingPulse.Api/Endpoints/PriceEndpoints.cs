using TradingPulse.Application.Abstractions;

namespace TradingPulse.Api.Endpoints;

public static class PriceEndpoints
{
    public static IEndpointRouteBuilder MapPriceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/prices/{symbol}", GetLatestAsync);

        return app;
    }

    private static async Task<IResult> GetLatestAsync(string symbol, IPriceStateRepository repository, CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetLatestAsync(symbol, cancellationToken);
        return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
    }
}

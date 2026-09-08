using TradingPulse.Api.Contracts;
using TradingPulse.Application.Abstractions;

namespace TradingPulse.Api.Endpoints;

/// <summary>
/// Manages per-client API keys. Gated to the master key only by <c>ApiKeyMiddleware</c> (a
/// per-client key cannot reach these routes) - there's no bootstrap problem this way, since the
/// same dev key that's always worked for the rest of the API also works here.
/// </summary>
public static class ApiKeyEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/keys", CreateAsync);
        app.MapDelete("/api/admin/keys/{clientName}", RevokeAsync);
        app.MapGet("/api/admin/keys", ListAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(CreateApiKeyRequest request, IApiKeyRepository repository, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            return Results.BadRequest("ClientName is required.");
        }

        var clientName = request.ClientName.Trim();
        var rawKey = await repository.CreateAsync(clientName, cancellationToken);

        return Results.Ok(new CreateApiKeyResponse(clientName, rawKey));
    }

    private static async Task<IResult> RevokeAsync(string clientName, IApiKeyRepository repository, CancellationToken cancellationToken)
    {
        var revoked = await repository.RevokeAsync(clientName, cancellationToken);
        return revoked
            ? Results.NoContent()
            : Results.NotFound($"No active key found for client '{clientName}'.");
    }

    private static async Task<IResult> ListAsync(IApiKeyRepository repository, CancellationToken cancellationToken) =>
        Results.Ok(await repository.ListAsync(cancellationToken));
}

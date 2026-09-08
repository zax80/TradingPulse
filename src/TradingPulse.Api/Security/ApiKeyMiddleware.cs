using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Security;

namespace TradingPulse.Api.Security;

/// <summary>
/// Auth for the REST API: requests under <c>/api</c> must carry a matching <c>X-Api-Key</c>
/// header. Two kinds of key are accepted:
/// <list type="bullet">
/// <item>the configured "master" key (<c>Authentication:ApiKey</c>) - a single bootstrap
/// secret that can call every endpoint, including <c>/api/admin/keys</c></item>
/// <item>a per-client key minted via <c>POST /api/admin/keys</c> and resolved through
/// <see cref="IApiKeyRepository"/> - valid for ordinary <c>/api/**</c> calls, not for
/// <c>/api/admin/**</c>, so a client key can never mint or revoke other keys</item>
/// </list>
/// The resolved caller name is stashed on <see cref="HttpContext.Items"/> under
/// <see cref="ClientNameItemKey"/> for anything downstream that wants to log or attribute the
/// request. This still isn't a real auth system - no scopes, no per-endpoint permissions, no
/// key expiry, and the Blazor Server dashboard (not under <c>/api</c>) isn't gated by it at all.
/// See README Known Limitations for what's still missing.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, IApiKeyRepository apiKeyRepository)
{
    public const string ClientNameItemKey = "ApiClientName";

    private const string HeaderName = "X-Api-Key";
    private const string AdminPathPrefix = "/api/admin";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var masterKey = configuration["Authentication:ApiKey"];
        if (string.IsNullOrEmpty(masterKey))
        {
            // Unconfigured deployment: fail open (no gate) rather than fail closed and block
            // every request on a missing setting nobody would notice until orders stop flowing.
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided))
        {
            await RejectAsync(context);
            return;
        }

        var providedKey = provided.ToString();

        if (ApiKeyGenerator.ConstantTimeEquals(providedKey, masterKey))
        {
            context.Items[ClientNameItemKey] = "master";
            await next(context);
            return;
        }

        var isAdminPath = context.Request.Path.StartsWithSegments(AdminPathPrefix);
        if (isAdminPath)
        {
            // Only the master key manages API clients - a per-client key can never escalate
            // into minting or revoking other keys.
            await RejectAsync(context);
            return;
        }

        var clientName = await apiKeyRepository.ResolveAsync(providedKey, context.RequestAborted);
        if (clientName is null)
        {
            await RejectAsync(context);
            return;
        }

        context.Items[ClientNameItemKey] = clientName;
        await next(context);
    }

    private static async Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = $"Missing or invalid {HeaderName} header." });
    }
}

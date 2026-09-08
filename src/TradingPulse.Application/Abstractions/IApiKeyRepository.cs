namespace TradingPulse.Application.Abstractions;

/// <summary>Metadata for one issued API key. Never carries the raw key - only its owner and lifecycle timestamps.</summary>
public sealed record ApiKeyInfo(string ClientName, DateTimeOffset CreatedAtUtc, DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Per-client API credentials for the REST API. Backs <c>ApiKeyMiddleware</c> (every
/// <c>/api/**</c> request resolves its key here) and the <c>/api/admin/keys</c> management
/// endpoints. A configured "master" key (see <c>Authentication:ApiKey</c>) bypasses this store
/// entirely and is handled directly by the middleware - this interface only concerns
/// per-client keys minted after the fact.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Mints a new active key for <paramref name="clientName"/> and returns the raw key.
    /// The raw value is never persisted or retrievable again - only its hash is stored -
    /// so this is the one and only moment the caller sees it. Minting a second key for a
    /// name already in use is how key rotation works: both keys stay valid until the old
    /// one is explicitly revoked.
    /// </summary>
    Task<string> CreateAsync(string clientName, CancellationToken cancellationToken = default);

    /// <summary>Revokes every currently active key for <paramref name="clientName"/>. Returns false if none were active.</summary>
    Task<bool> RevokeAsync(string clientName, CancellationToken cancellationToken = default);

    /// <summary>Resolves a raw key to its owning client name, or null if it doesn't match any active (non-revoked) key.</summary>
    Task<string?> ResolveAsync(string rawKey, CancellationToken cancellationToken = default);

    /// <summary>Lists every key ever issued (active and revoked), for the admin listing endpoint. Never exposes raw key material.</summary>
    Task<IReadOnlyList<ApiKeyInfo>> ListAsync(CancellationToken cancellationToken = default);
}

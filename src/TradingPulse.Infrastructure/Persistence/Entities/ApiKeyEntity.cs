namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Persistence model for one issued API key. See <c>Repositories.EfApiKeyRepository</c>.</summary>
public sealed class ApiKeyEntity
{
    public Guid Id { get; set; }
    public required string ClientName { get; set; }

    /// <summary>SHA-256 hex of the raw key (see <c>Security.ApiKeyGenerator.Hash</c>). The raw key itself is never stored.</summary>
    public required string KeyHash { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Null while active. Set once, never cleared - a revoked key stays revoked.</summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Persistence.Entities;
using TradingPulse.Infrastructure.Security;

namespace TradingPulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed per-client API keys. <see cref="ResolveAsync"/> is on the request path for
/// every <c>/api/**</c> call, so - same pattern as <see cref="EfTradingRulesRepository"/> - active
/// keys are cached as a lock-free, <see cref="Volatile"/>-swapped hash-to-client map and only a
/// mutation (<see cref="CreateAsync"/>/<see cref="RevokeAsync"/>) touches the database and
/// invalidates it. Only the SHA-256 hash of a key is ever read back or persisted; the raw value
/// lives only in the response to the request that minted it.
/// </summary>
public sealed class EfApiKeyRepository(IDbContextFactory<TradingPulseDbContext> dbContextFactory) : IApiKeyRepository
{
    private ImmutableDictionary<string, string>? _cachedActiveKeys;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public async Task<string> CreateAsync(string clientName, CancellationToken cancellationToken = default)
    {
        var rawKey = ApiKeyGenerator.GenerateRawKey();

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ApiKeys.Add(new ApiKeyEntity
        {
            Id = Guid.NewGuid(),
            ClientName = clientName,
            KeyHash = ApiKeyGenerator.Hash(rawKey),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return rawKey;
    }

    public async Task<bool> RevokeAsync(string clientName, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var active = await db.ApiKeys
            .Where(k => k.ClientName == clientName && k.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (active.Count == 0)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var key in active)
        {
            key.RevokedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return true;
    }

    public async Task<string?> ResolveAsync(string rawKey, CancellationToken cancellationToken = default)
    {
        var cache = await GetOrLoadCacheAsync(cancellationToken);
        return cache.TryGetValue(ApiKeyGenerator.Hash(rawKey), out var clientName) ? clientName : null;
    }

    public async Task<IReadOnlyList<ApiKeyInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.ApiKeys
            .OrderBy(k => k.ClientName)
            .ThenBy(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyInfo(k.ClientName, k.CreatedAtUtc, k.RevokedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private void InvalidateCache() => Volatile.Write(ref _cachedActiveKeys, null);

    private async Task<ImmutableDictionary<string, string>> GetOrLoadCacheAsync(CancellationToken cancellationToken)
    {
        var cached = Volatile.Read(ref _cachedActiveKeys);
        if (cached is not null)
        {
            return cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have finished loading (or a mutation may have invalidated a
            // load already in flight) while this one waited on the lock.
            cached = Volatile.Read(ref _cachedActiveKeys);
            if (cached is not null)
            {
                return cached;
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var active = await db.ApiKeys
                .Where(k => k.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var map = active.ToImmutableDictionary(k => k.KeyHash, k => k.ClientName);
            Volatile.Write(ref _cachedActiveKeys, map);
            return map;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}

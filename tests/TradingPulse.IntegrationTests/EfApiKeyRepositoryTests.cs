using TradingPulse.Infrastructure.Persistence.Repositories;

namespace TradingPulse.IntegrationTests;

/// <summary>
/// Exercises <see cref="EfApiKeyRepository"/> against real Postgres: the unique index on
/// <c>KeyHash</c>, and that <see cref="EfApiKeyRepository.RevokeAsync"/> invalidating the
/// in-process cache actually forces the next <see cref="EfApiKeyRepository.ResolveAsync"/> to see
/// the database's current state rather than a stale cached map.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfApiKeyRepositoryTests(PostgresFixture fixture)
{
    private readonly EfApiKeyRepository _repository = new(fixture.DbContextFactory);

    [Fact]
    public async Task CreateAsync_Then_ResolveAsync_RoundTrips_ToTheClientName()
    {
        var clientName = $"it-client-{Guid.NewGuid():N}";
        var rawKey = await _repository.CreateAsync(clientName);

        Assert.StartsWith("tpk_", rawKey);
        Assert.Equal(clientName, await _repository.ResolveAsync(rawKey));
    }

    [Fact]
    public async Task ResolveAsync_UnknownKey_ReturnsNull()
    {
        var resolved = await _repository.ResolveAsync($"tpk_does-not-exist-{Guid.NewGuid():N}");
        Assert.Null(resolved);
    }

    [Fact]
    public async Task RevokeAsync_StopsTheKeyFromResolving_Immediately()
    {
        var clientName = $"it-client-{Guid.NewGuid():N}";
        var rawKey = await _repository.CreateAsync(clientName);
        Assert.Equal(clientName, await _repository.ResolveAsync(rawKey));

        var revoked = await _repository.RevokeAsync(clientName);
        Assert.True(revoked);

        // Same repository instance - only passes if RevokeAsync actually invalidates the
        // in-process cache, not just the database row.
        Assert.Null(await _repository.ResolveAsync(rawKey));
    }

    [Fact]
    public async Task RevokeAsync_UnknownClient_ReturnsFalse()
    {
        var revoked = await _repository.RevokeAsync($"never-existed-{Guid.NewGuid():N}");
        Assert.False(revoked);
    }

    [Fact]
    public async Task Rotation_TwoActiveKeysForTheSameClient_BothResolve_UntilRevoked()
    {
        var clientName = $"it-client-{Guid.NewGuid():N}";
        var firstKey = await _repository.CreateAsync(clientName);
        var secondKey = await _repository.CreateAsync(clientName);

        Assert.Equal(clientName, await _repository.ResolveAsync(firstKey));
        Assert.Equal(clientName, await _repository.ResolveAsync(secondKey));

        await _repository.RevokeAsync(clientName);

        Assert.Null(await _repository.ResolveAsync(firstKey));
        Assert.Null(await _repository.ResolveAsync(secondKey));
    }

    [Fact]
    public async Task ListAsync_ReturnsMetadataOnly_NeverTheRawKey()
    {
        var clientName = $"it-client-{Guid.NewGuid():N}";
        await _repository.CreateAsync(clientName);

        var listed = await _repository.ListAsync();
        var entry = Assert.Single(listed, k => k.ClientName == clientName);
        Assert.Null(entry.RevokedAtUtc);
    }
}

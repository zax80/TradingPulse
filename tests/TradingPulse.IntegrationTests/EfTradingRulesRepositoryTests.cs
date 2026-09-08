using TradingPulse.Domain;
using TradingPulse.Infrastructure.Persistence.Repositories;

namespace TradingPulse.IntegrationTests;

/// <summary>
/// Exercises <see cref="EfTradingRulesRepository"/> against real Postgres. Unlike the order/API
/// key tests, these three deliberately touch the *same* singleton row (trading_rules, id=1) - so
/// each test creates its own repository instance and asserts against what a *fresh* instance sees,
/// proving a write actually reached the database and not just the writer's own in-process cache.
/// Sharing <see cref="PostgresCollection"/> with every other test class keeps these from running
/// concurrently with anything else that touches the same database.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfTradingRulesRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetCurrentAsync_ReturnsUsableRules_SeedingDefaultsIfNoneExistYet()
    {
        var repository = new EfTradingRulesRepository(fixture.DbContextFactory);
        var rules = await repository.GetCurrentAsync();

        Assert.True(rules.MaxNotionalPerOrder > 0);
        Assert.True(rules.MaxQuantityPerOrder > 0);
        Assert.True(rules.AutoTradingSpreadPercentThreshold > 0);
    }

    [Fact]
    public async Task SaveAsync_Persists_And_IsVisibleTo_ANewRepositoryInstance()
    {
        var writer = new EfTradingRulesRepository(fixture.DbContextFactory);
        var updated = TradingRules.Default with
        {
            MaxNotionalPerOrder = 123_456m,
            MaxQuantityPerOrder = 7_777m,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await writer.SaveAsync(updated);

        // A second instance starts with no in-memory cache, so this only passes if the write
        // actually reached the database.
        var reader = new EfTradingRulesRepository(fixture.DbContextFactory);
        var reloaded = await reader.GetCurrentAsync();

        Assert.Equal(123_456m, reloaded.MaxNotionalPerOrder);
        Assert.Equal(7_777m, reloaded.MaxQuantityPerOrder);
    }

    [Fact]
    public async Task SaveAsync_RoundTrips_SymbolWhitelist_ThroughTheJsonColumn()
    {
        var writer = new EfTradingRulesRepository(fixture.DbContextFactory);
        var updated = TradingRules.Default with
        {
            SymbolWhitelistEnabled = true,
            SymbolWhitelist = new List<string> { "EURUSD", "GBPUSD" },
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await writer.SaveAsync(updated);

        var reader = new EfTradingRulesRepository(fixture.DbContextFactory);
        var reloaded = await reader.GetCurrentAsync();

        Assert.True(reloaded.SymbolWhitelistEnabled);
        Assert.Equal(["EURUSD", "GBPUSD"], reloaded.SymbolWhitelist);
    }
}

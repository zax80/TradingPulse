using Microsoft.EntityFrameworkCore;
using TradingPulse.Domain;
using TradingPulse.Infrastructure.Persistence.Repositories;

namespace TradingPulse.IntegrationTests;

/// <summary>
/// Exercises <see cref="EfPriceSnapshotStore"/> against real Postgres. <see cref="IPriceSnapshotStore"/>
/// (see TradingPulse.Application) has no read method of its own - it exists purely to be written
/// to by <c>PriceStatePersistenceService</c> - so these tests read the row back through a fresh
/// <see cref="TradingPulseDbContext"/> directly, the way a restart-recovery path would.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfPriceSnapshotStoreTests(PostgresFixture fixture)
{
    private readonly EfPriceSnapshotStore _store = new(fixture.DbContextFactory);

    [Fact]
    public async Task SaveLatestAsync_Insert_Then_Update_UpsertsTheSameRow()
    {
        var symbol = $"IT{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var first = new PriceSnapshot(symbol, 1.1000m, 1.1002m, 1.1001m, 0.0002m, 0.0182m, null, DateTimeOffset.UtcNow);

        await _store.SaveLatestAsync([first]);
        await AssertStoredBidAsync(symbol, expectedBid: 1.1000m);

        var second = first with
        {
            BidPrice = 1.2000m,
            AskPrice = 1.2002m,
            CurrentMarketPrice = 1.2001m,
            PreviousMarketPrice = 1.1001m,
            Timestamp = DateTimeOffset.UtcNow,
        };
        await _store.SaveLatestAsync([second]);

        // Same symbol, second call - must update the existing row (upsert), not insert a
        // duplicate. Read through a brand-new DbContext, not the store itself, so this only
        // passes if the write actually reached the database.
        await AssertStoredBidAsync(symbol, expectedBid: 1.2000m);

        await using var db = await fixture.DbContextFactory.CreateDbContextAsync();
        Assert.Equal(1, await db.PriceStates.CountAsync(p => p.Symbol == symbol));
    }

    [Fact]
    public async Task SaveLatestAsync_MultipleSymbolsInOneCall_AreAllPersisted()
    {
        var symbolA = $"IT{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var symbolB = $"IT{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var snapshots = new[]
        {
            new PriceSnapshot(symbolA, 1.0m, 1.001m, 1.0005m, 0.001m, 0.1m, null, DateTimeOffset.UtcNow),
            new PriceSnapshot(symbolB, 2.0m, 2.002m, 2.001m, 0.002m, 0.1m, null, DateTimeOffset.UtcNow),
        };

        await _store.SaveLatestAsync(snapshots);

        await AssertStoredBidAsync(symbolA, expectedBid: 1.0m);
        await AssertStoredBidAsync(symbolB, expectedBid: 2.0m);
    }

    private async Task AssertStoredBidAsync(string symbol, decimal expectedBid)
    {
        await using var db = await fixture.DbContextFactory.CreateDbContextAsync();
        var row = await db.PriceStates.FindAsync(symbol);
        Assert.NotNull(row);
        Assert.Equal(expectedBid, row!.BidPrice);
    }
}

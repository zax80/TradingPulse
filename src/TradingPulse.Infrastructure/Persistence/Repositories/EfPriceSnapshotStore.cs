using Microsoft.EntityFrameworkCore;
using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;
using TradingPulse.Infrastructure.Persistence.Entities;

namespace TradingPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core-backed durable price store. Written to periodically by <see cref="Pricing.PriceStatePersistenceService"/>, never on the tick hot path.</summary>
public sealed class EfPriceSnapshotStore(IDbContextFactory<TradingPulseDbContext> dbContextFactory) : IPriceSnapshotStore
{
    public async Task SaveLatestAsync(IReadOnlyList<PriceSnapshot> snapshots, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (var snapshot in snapshots)
        {
            var entity = await db.PriceStates.FindAsync([snapshot.Symbol], cancellationToken);
            if (entity is null)
            {
                db.PriceStates.Add(PriceStateEntity.FromDomain(snapshot));
            }
            else
            {
                entity.UpdateFrom(snapshot);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

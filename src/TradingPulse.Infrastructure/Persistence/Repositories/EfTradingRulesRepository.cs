using Microsoft.EntityFrameworkCore;
using TradingPulse.Application.Abstractions;
using TradingPulse.Domain;
using TradingPulse.Infrastructure.Persistence.Entities;

namespace TradingPulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed trading rules, single row keyed by <see cref="SingletonRowId"/>.
/// Reads still come from an in-memory cache loaded once and swapped
/// lock-free on save (same pattern as Day 3's in-memory version), so
/// evaluating an order never waits on a database round trip - only
/// <see cref="SaveAsync"/> does.
/// </summary>
public sealed class EfTradingRulesRepository(IDbContextFactory<TradingPulseDbContext> dbContextFactory) : ITradingRulesRepository
{
    private const int SingletonRowId = 1;

    private TradingRules? _cached;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public async Task<TradingRules> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref _cached);
        if (cached is not null)
        {
            return cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have finished loading while this one waited on the lock.
            cached = Volatile.Read(ref _cached);
            if (cached is not null)
            {
                return cached;
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.TradingRules.FindAsync([SingletonRowId], cancellationToken);

            TradingRules rules;
            if (entity is null)
            {
                rules = TradingRules.Default;
                db.TradingRules.Add(TradingRulesEntity.FromDomain(SingletonRowId, rules));
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                rules = entity.ToDomain();
            }

            Volatile.Write(ref _cached, rules);
            return rules;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task SaveAsync(TradingRules rules, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.TradingRules.FindAsync([SingletonRowId], cancellationToken);
        if (entity is null)
        {
            db.TradingRules.Add(TradingRulesEntity.FromDomain(SingletonRowId, rules));
        }
        else
        {
            entity.UpdateFrom(rules);
        }

        await db.SaveChangesAsync(cancellationToken);

        Volatile.Write(ref _cached, rules);
    }
}

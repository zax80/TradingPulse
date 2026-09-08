using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TradingPulse.Infrastructure.Persistence;

namespace TradingPulse.IntegrationTests;

/// <summary>
/// Starts one real, throwaway Postgres container (via Testcontainers) for the whole test run and
/// exposes an <see cref="IDbContextFactory{TradingPulseDbContext}"/> wired against it - the exact
/// type every <c>Ef*Repository</c> takes in production, so these tests exercise the real EF Core
/// mapping, SQL translation, and constraints (unique index, precision, JSON columns) rather than
/// a mock or an in-memory provider that would silently accept queries Postgres can't run.
///
/// Requires a local Docker daemon. There's nothing this fixture can do if one isn't running -
/// xunit fails the collection with Testcontainers' own connection error, which is the intended
/// signal ("run `docker` first"), not a bug to swallow.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private ServiceProvider? _services;

    public IDbContextFactory<TradingPulseDbContext> DbContextFactory =>
        _services?.GetRequiredService<IDbContextFactory<TradingPulseDbContext>>()
            ?? throw new InvalidOperationException("PostgresFixture.InitializeAsync hasn't run yet.");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("tradingpulse_it")
            .WithUsername("tradingpulse")
            .WithPassword("tradingpulse")
            .Build();

        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<TradingPulseDbContext>(options =>
            options.UseNpgsql(_container.GetConnectionString()));
        _services = services.BuildServiceProvider();

        // Same bootstrap Program.cs uses in production (see README Known Limitations for why
        // this is EnsureCreated, not migrations) - applied once, against the fresh container.
        await using var db = await DbContextFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// All integration test classes share this one collection so xunit runs them sequentially
/// against the single shared container instead of spinning one up per class (slow) or letting
/// classes race each other on shared singleton rows (trading_rules) inside it.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

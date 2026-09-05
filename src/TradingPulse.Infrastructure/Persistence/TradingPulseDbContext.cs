using Microsoft.EntityFrameworkCore;
using TradingPulse.Infrastructure.Persistence.Entities;

namespace TradingPulse.Infrastructure.Persistence;

public sealed class TradingPulseDbContext(DbContextOptions<TradingPulseDbContext> options) : DbContext(options)
{
    public DbSet<PriceStateEntity> PriceStates => Set<PriceStateEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderDecisionEntity> OrderDecisions => Set<OrderDecisionEntity>();
    public DbSet<TradingRulesEntity> TradingRules => Set<TradingRulesEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceStateEntity>(entity =>
        {
            entity.ToTable("price_states");
            entity.HasKey(e => e.Symbol);
            entity.Property(e => e.Symbol).HasMaxLength(20);
            entity.Property(e => e.BidPrice).HasPrecision(18, 8);
            entity.Property(e => e.AskPrice).HasPrecision(18, 8);
            entity.Property(e => e.CurrentMarketPrice).HasPrecision(18, 8);
            entity.Property(e => e.Spread).HasPrecision(18, 8);
            entity.Property(e => e.SpreadPercent).HasPrecision(18, 8);
            entity.Property(e => e.PreviousMarketPrice).HasPrecision(18, 8);
        });

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientOrderId).HasMaxLength(100);
            entity.Property(e => e.Symbol).HasMaxLength(20);
            entity.Property(e => e.Side).HasMaxLength(10);
            entity.Property(e => e.Type).HasMaxLength(10);
            entity.Property(e => e.Origin).HasMaxLength(20);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.ClientOrderId);
            entity.HasIndex(e => e.SubmittedAt);
        });

        modelBuilder.Entity<OrderDecisionEntity>(entity =>
        {
            entity.ToTable("order_decisions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(10);
            // No FK/navigation to OrderEntity by design - the two are written together by
            // EfOrderRepository and only ever joined manually on OrderId, so a formal
            // relationship would add mapping surface without buying anything.
            entity.HasIndex(e => e.OrderId);
        });

        modelBuilder.Entity<TradingRulesEntity>(entity =>
        {
            entity.ToTable("trading_rules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaxNotionalPerOrder).HasPrecision(18, 8);
            entity.Property(e => e.MaxQuantityPerOrder).HasPrecision(18, 8);
            entity.Property(e => e.PriceDeviationThresholdPercent).HasPrecision(9, 4);
            entity.Property(e => e.AutoTradingSpreadPercentThreshold).HasPrecision(9, 4);
        });
    }
}

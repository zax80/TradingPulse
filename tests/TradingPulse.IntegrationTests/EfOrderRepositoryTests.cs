using TradingPulse.Application.Models;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;
using TradingPulse.Infrastructure.Persistence.Repositories;

namespace TradingPulse.IntegrationTests;

/// <summary>
/// Exercises <see cref="EfOrderRepository"/> against a real Postgres instance - the join query in
/// <see cref="EfOrderRepository.GetByClientOrderIdAsync"/>/<see cref="EfOrderRepository.GetHistoryAsync"/>,
/// the <c>.ToLower()</c> symbol-filter translation the CA1862 suppression depends on actually
/// working, and the <see cref="RejectionReason"/> JSON round trip through
/// <c>OrderDecisionEntity.RejectionReasonsJson</c>. Every test uses a unique symbol/ClientOrderId
/// (via <see cref="Guid.NewGuid"/>) so tests stay independent even though they share one database.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfOrderRepositoryTests(PostgresFixture fixture)
{
    private readonly EfOrderRepository _repository = new(fixture.DbContextFactory);

    [Fact]
    public async Task AddAsync_Then_GetByClientOrderIdAsync_RoundTrips_AcceptedOrder()
    {
        var clientOrderId = $"IT-{Guid.NewGuid():N}";
        var order = Order.CreateUserSubmitted(clientOrderId, "EURUSD", OrderSide.Buy, OrderType.Limit, 1.1000m, 100m, DateTimeOffset.UtcNow);
        var decision = OrderDecision.Accept(order.Id, DateTimeOffset.UtcNow);

        await _repository.AddAsync(order, decision);
        var stored = await _repository.GetByClientOrderIdAsync(clientOrderId);

        Assert.NotNull(stored);
        Assert.Equal(order.Id, stored!.Order.Id);
        Assert.Equal(order.Symbol, stored.Order.Symbol);
        Assert.Equal(order.Price, stored.Order.Price);
        Assert.Equal(order.Quantity, stored.Order.Quantity);
        Assert.Equal(DecisionStatus.Accepted, stored.Decision.Status);
        Assert.Empty(stored.Decision.RejectionReasons);
    }

    [Fact]
    public async Task AddAsync_Then_GetByClientOrderIdAsync_RoundTrips_RejectedOrder_WithStructuredReasons()
    {
        var clientOrderId = $"IT-{Guid.NewGuid():N}";
        var order = Order.CreateUserSubmitted(clientOrderId, "GBPUSD", OrderSide.Sell, OrderType.Limit, 1.3000m, 999_999m, DateTimeOffset.UtcNow);
        var reasons = new List<RejectionReason>
        {
            new(RejectionReasonCode.MaxNotionalExceeded, "Notional exceeds the configured maximum."),
            new(RejectionReasonCode.MaxQuantityExceeded, "Quantity exceeds the configured maximum."),
        };
        var decision = OrderDecision.Reject(order.Id, reasons, DateTimeOffset.UtcNow);

        await _repository.AddAsync(order, decision);
        var stored = await _repository.GetByClientOrderIdAsync(clientOrderId);

        Assert.NotNull(stored);
        Assert.Equal(DecisionStatus.Rejected, stored!.Decision.Status);
        Assert.Equal(2, stored.Decision.RejectionReasons.Count);
        // Proves the JsonStringEnumConverter round-trip through RejectionReasonsJson survives a
        // real database write + fresh read, not just an in-process (de)serialize call.
        Assert.Contains(stored.Decision.RejectionReasons, r => r.Code == RejectionReasonCode.MaxNotionalExceeded);
        Assert.Contains(stored.Decision.RejectionReasons, r => r.Code == RejectionReasonCode.MaxQuantityExceeded);
    }

    [Fact]
    public async Task GetByClientOrderIdAsync_UnknownId_ReturnsNull()
    {
        var stored = await _repository.GetByClientOrderIdAsync($"IT-does-not-exist-{Guid.NewGuid():N}");
        Assert.Null(stored);
    }

    [Fact]
    public async Task GetBySymbolAsync_And_GetHistoryAsync_SymbolFilter_AreCaseInsensitive()
    {
        var symbol = UniqueSymbol();
        var order = Order.CreateUserSubmitted($"IT-{Guid.NewGuid():N}", symbol, OrderSide.Buy, OrderType.Limit, 1.0m, 10m, DateTimeOffset.UtcNow);
        await _repository.AddAsync(order, OrderDecision.Accept(order.Id, DateTimeOffset.UtcNow));

        // Exercises the .ToLower() EF query translation directly (see EfOrderRepository's CA1862
        // suppression note) - a lowercase filter must still match the uppercase stored symbol.
        var bySymbol = await _repository.GetBySymbolAsync(symbol.ToLowerInvariant());
        Assert.Single(bySymbol);
        Assert.Equal(order.Id, bySymbol[0].Order.Id);

        var history = await _repository.GetHistoryAsync(new OrderHistoryFilter(Symbol: symbol.ToLowerInvariant()));
        Assert.Single(history);
    }

    [Fact]
    public async Task GetHistoryAsync_Filters_By_Side_And_Status()
    {
        var symbol = UniqueSymbol();
        var accepted = Order.CreateUserSubmitted($"IT-{Guid.NewGuid():N}", symbol, OrderSide.Buy, OrderType.Limit, 1.0m, 10m, DateTimeOffset.UtcNow);
        var rejected = Order.CreateUserSubmitted($"IT-{Guid.NewGuid():N}", symbol, OrderSide.Sell, OrderType.Limit, 1.0m, 10m, DateTimeOffset.UtcNow);
        await _repository.AddAsync(accepted, OrderDecision.Accept(accepted.Id, DateTimeOffset.UtcNow));
        await _repository.AddAsync(
            rejected,
            OrderDecision.Reject(rejected.Id, [new RejectionReason(RejectionReasonCode.NoCurrentPrice, "no price yet")], DateTimeOffset.UtcNow));

        var buys = await _repository.GetHistoryAsync(new OrderHistoryFilter(Symbol: symbol, Side: OrderSide.Buy));
        Assert.Single(buys);
        Assert.Equal(accepted.Id, buys[0].Order.Id);

        var rejectedOnly = await _repository.GetHistoryAsync(new OrderHistoryFilter(Symbol: symbol, Status: DecisionStatus.Rejected));
        Assert.Single(rejectedOnly);
        Assert.Equal(rejected.Id, rejectedOnly[0].Order.Id);
    }

    private static string UniqueSymbol() => $"IT{Guid.NewGuid():N}"[..8].ToUpperInvariant();
}

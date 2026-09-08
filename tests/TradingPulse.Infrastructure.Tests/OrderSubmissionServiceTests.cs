using TradingPulse.Domain;
using TradingPulse.Domain.Enums;
using TradingPulse.Infrastructure.Orders;
using TradingPulse.Infrastructure.Pricing;
using TradingPulse.Infrastructure.Rules;
using TradingPulse.Infrastructure.Trading;

namespace TradingPulse.Infrastructure.Tests;

/// <summary>
/// Covers the idempotent-retry behavior added to <see cref="OrderSubmissionService"/>:
/// an identical resubmission of a <see cref="ClientOrderId"/> returns the original
/// decision without re-evaluating or persisting, while a *conflicting* reuse of the
/// same id (different order details) still goes through the duplicate-id rule.
/// </summary>
public class OrderSubmissionServiceTests
{
    private readonly InMemoryOrderRepository _orderRepository = new();
    private readonly InMemoryPriceStateRepository _priceStateRepository = new();
    private readonly InMemoryTradingRulesRepository _rulesRepository = new();
    private readonly OrderSubmissionService _service;

    public OrderSubmissionServiceTests()
    {
        _service = new OrderSubmissionService(_priceStateRepository, _rulesRepository, new TradingRulesEngine(), _orderRepository);
    }

    [Fact]
    public async Task SubmitAsync_Accepts_And_Persists_NewOrder()
    {
        await SeedPriceAsync("EURUSD", 1.10m);
        var order = CreateOrder("ORD-1", price: 1.10m, quantity: 100m);

        var decision = await _service.SubmitAsync(order);

        Assert.Equal(DecisionStatus.Accepted, decision.Status);
        var stored = await _orderRepository.GetByClientOrderIdAsync("ORD-1");
        Assert.NotNull(stored);
        Assert.Equal(order.Id, stored!.Order.Id);
    }

    [Fact]
    public async Task SubmitAsync_IdenticalRetry_ReturnsOriginalDecision_WithoutPersistingAgain()
    {
        await SeedPriceAsync("EURUSD", 1.10m);
        var first = CreateOrder("ORD-2", price: 1.10m, quantity: 100m);
        var firstDecision = await _service.SubmitAsync(first);

        // Same ClientOrderId, same Symbol/Side/Type/Price/Quantity - a genuine retry
        // (e.g. the client never saw the first response), just a different Order.Id/timestamp.
        var retry = CreateOrder("ORD-2", price: 1.10m, quantity: 100m);
        var retryDecision = await _service.SubmitAsync(retry);

        Assert.Equal(firstDecision.Id, retryDecision.Id);
        Assert.Equal(firstDecision.OrderId, retryDecision.OrderId);
        Assert.NotEqual(retry.Id, retryDecision.OrderId); // decision still points at the original order, not the retry

        var history = await _orderRepository.GetHistoryAsync(new Application.Models.OrderHistoryFilter(Symbol: "EURUSD"));
        Assert.Single(history); // no second row written for the retry
    }

    [Fact]
    public async Task SubmitAsync_ConflictingReuse_OfClientOrderId_IsRejected_AndPersistedSeparately()
    {
        await SeedPriceAsync("EURUSD", 1.10m);
        var first = CreateOrder("ORD-3", price: 1.10m, quantity: 100m);
        await _service.SubmitAsync(first);

        // Same ClientOrderId, but a different Price - not a retry of the same request, so it
        // must not be silently treated as one; the duplicate-id rule rejects it.
        var conflicting = CreateOrder("ORD-3", price: 1.101m, quantity: 100m);
        var decision = await _service.SubmitAsync(conflicting);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Contains(decision.RejectionReasons, r => r.Code == RejectionReasonCode.DuplicateClientOrderId);

        var history = await _orderRepository.GetHistoryAsync(new Application.Models.OrderHistoryFilter(Symbol: "EURUSD"));
        Assert.Equal(2, history.Count); // the conflicting attempt is persisted as its own (rejected) entry
    }

    private async Task SeedPriceAsync(string symbol, decimal midPrice)
    {
        // Bid/ask straddling midPrice by a cent keeps CurrentMarketPrice == midPrice for these tests.
        PriceUpdate.TryCreate(symbol, midPrice - 0.001m, midPrice + 0.001m, DateTimeOffset.UtcNow, out var tick, out _);
        await _priceStateRepository.ApplyTickAsync(tick);
    }

    private static Order CreateOrder(string clientOrderId, decimal price, decimal quantity, string symbol = "EURUSD") =>
        Order.CreateUserSubmitted(clientOrderId, symbol, OrderSide.Buy, OrderType.Limit, price, quantity, DateTimeOffset.UtcNow);
}

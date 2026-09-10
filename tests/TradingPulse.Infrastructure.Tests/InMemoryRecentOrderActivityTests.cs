using TradingPulse.Application.Models;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;
using TradingPulse.Infrastructure.Orders;

namespace TradingPulse.Infrastructure.Tests;

public class InMemoryRecentOrderActivityTests
{
    private readonly InMemoryRecentOrderActivity _activity = new();

    [Fact]
    public void GetRecent_ReturnsEmpty_WhenNothingRecorded()
    {
        Assert.Empty(_activity.GetRecent(20));
    }

    [Fact]
    public void GetRecent_ReturnsNewestFirst()
    {
        _activity.Record(CreateEntry("ORD-1"));
        _activity.Record(CreateEntry("ORD-2"));
        _activity.Record(CreateEntry("ORD-3"));

        var recent = _activity.GetRecent(10);

        Assert.Equal(["ORD-3", "ORD-2", "ORD-1"], recent.Select(e => e.Order.ClientOrderId));
    }

    [Fact]
    public void GetRecent_RespectsRequestedCount_EvenWhenMoreAreHeld()
    {
        for (var i = 0; i < 5; i++)
        {
            _activity.Record(CreateEntry($"ORD-{i}"));
        }

        Assert.Equal(2, _activity.GetRecent(2).Count);
    }

    [Fact]
    public void Record_IsBounded_OldestEntriesFallOff()
    {
        // MaxEntries is 50 (internal, not exposed) - recording well past that must not grow
        // unbounded, and the oldest entries are the ones dropped.
        for (var i = 0; i < 60; i++)
        {
            _activity.Record(CreateEntry($"ORD-{i}"));
        }

        var recent = _activity.GetRecent(100);

        Assert.Equal(50, recent.Count);
        Assert.Equal("ORD-59", recent[0].Order.ClientOrderId); // newest survives
        Assert.DoesNotContain(recent, e => e.Order.ClientOrderId == "ORD-0"); // oldest fell off
    }

    private static OrderHistoryEntry CreateEntry(string clientOrderId)
    {
        var order = Order.CreateUserSubmitted(
            clientOrderId, "EURUSD", OrderSide.Buy, OrderType.Limit, price: 1.10m, quantity: 100m, submittedAt: DateTimeOffset.UtcNow);
        var decision = OrderDecision.Accept(order.Id, DateTimeOffset.UtcNow);

        return new OrderHistoryEntry(order, decision);
    }
}

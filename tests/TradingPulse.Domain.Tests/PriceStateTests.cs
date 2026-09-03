using System;
using System.Collections.Generic;
using System.Text;
using TradingPulse.Domain;

public class PriceStateTests
{
    [Fact]
    public void ToSnapshot_ReturnsNull_BeforeAnyTick()
    {
        var state = PriceState.CreateEmpty("EURUSD");

        Assert.Null(state.ToSnapshot());
    }

    [Fact]
    public void Apply_ComputesDerivedValues()
    {
        var state = PriceState.CreateEmpty("EURUSD");
        var timestamp = DateTimeOffset.UtcNow;
        var update = CreateUpdate("EURUSD", bid: 99m, ask: 101m, timestamp);

        var snapshot = state.Apply(update);

        Assert.Equal(100m, snapshot.CurrentMarketPrice);
        Assert.Equal(2m, snapshot.Spread);
        Assert.Equal(2.0m, snapshot.SpreadPercent);
        Assert.Equal(timestamp, snapshot.Timestamp);
    }

    [Fact]
    public void Apply_LeavesPreviousMarketPriceNull_OnFirstTick()
    {
        var state = PriceState.CreateEmpty("EURUSD");
        var update = CreateUpdate("EURUSD", bid: 99m, ask: 101m, DateTimeOffset.UtcNow);

        var snapshot = state.Apply(update);

        Assert.Null(snapshot.PreviousMarketPrice);
    }

    [Fact]
    public void Apply_SetsPreviousMarketPrice_ToPriorCurrentMarketPrice()
    {
        var state = PriceState.CreateEmpty("EURUSD");
        state.Apply(CreateUpdate("EURUSD", bid: 99m, ask: 101m, DateTimeOffset.UtcNow)); // mid = 100

        var second = state.Apply(CreateUpdate("EURUSD", bid: 100m, ask: 102m, DateTimeOffset.UtcNow)); // mid = 101

        Assert.Equal(101m, second.CurrentMarketPrice);
        Assert.Equal(100m, second.PreviousMarketPrice);
    }

    [Fact]
    public void Apply_UpdatesToSnapshot_AfterEachTick()
    {
        var state = PriceState.CreateEmpty("EURUSD");
        state.Apply(CreateUpdate("EURUSD", bid: 99m, ask: 101m, DateTimeOffset.UtcNow));

        var snapshot = state.ToSnapshot();

        Assert.NotNull(snapshot);
        Assert.Equal(100m, snapshot!.Value.CurrentMarketPrice);
    }

    [Fact]
    public void Apply_Throws_WhenSymbolDoesNotMatch()
    {
        var state = PriceState.CreateEmpty("EURUSD");
        var update = CreateUpdate("GBPUSD", bid: 1.27m, ask: 1.28m, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => state.Apply(update));
    }

    private static PriceUpdate CreateUpdate(string symbol, decimal bid, decimal ask, DateTimeOffset timestamp)
    {
        var created = PriceUpdate.TryCreate(symbol, bid, ask, timestamp, out var update, out var error);
        Assert.True(created, error);
        return update;
    }
}

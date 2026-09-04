# TradingPulse — Pricing Engine & Trading Rules Service

Simulates a multi-symbol market data feed, validates/auto-generates trading orders against configurable rules, persists results, exposes a REST API. Interview take-home — see the task brief for full requirements.

**Status: Day 3 of 5.** Trading rules engine, spread-based auto-trading, wired end to end (tick → auto-order → rules → decision persisted). Rules and orders are still in-memory — EF Core persistence lands Day 4.

## How to Run

**Prerequisites:** .NET 10 SDK, Visual Studio 2022 (17.14+) or the `dotnet` CLI.

**Visual Studio:** open `TradingPulse.slnx` → `TradingPulse.Api` is already the startup project → F5.

**CLI:**
```bash
dotnet build
dotnet run --project src/TradingPulse.Api
```
Open `http://localhost:<port>/health` → `{"status":"healthy","service":"TradingPulse.Api"}`.

`GET /debug/prices` shows the pricing engine running live (10 simulated instruments, ticking every 200–600ms). `GET /debug/orders` shows auto-generated orders and their decisions. Both temporary — replaced by the real API on Day 4.

`dotnet test` runs the unit tests (`tests/TradingPulse.Domain.Tests`, `tests/TradingPulse.Infrastructure.Tests`).

## Structure

```
TradingPulse.slnx
├── Directory.Build.props
├── src/
│   ├── TradingPulse.Domain          — entities, value objects, enums. No dependencies.
│   ├── TradingPulse.Application     — interfaces + DTOs. Depends on Domain only.
│   ├── TradingPulse.Infrastructure  — implementations (pricing engine, tick consumer, rules engine, auto-trading; EF Core persistence Day 4). Depends on Application + Domain.
│   └── TradingPulse.Api             — ASP.NET Core host, composition root. Depends on all above.
└── tests/
    ├── TradingPulse.Domain.Tests          — xUnit, Domain layer.
    └── TradingPulse.Infrastructure.Tests  — xUnit, rules engine + auto-trading.
```
Dependencies point inward (`Api → Infrastructure → Application → Domain`); Domain and Application never reference ASP.NET Core or EF Core.

## Design Decisions

**Day 1**
- **.NET 10, not .NET 9.** .NET 9 is past end-of-support; .NET 10 (LTS) satisfies "9+" and matches current tooling. `TargetFramework` lives in one place (`Directory.Build.props`).
- **`PriceUpdate`/`PriceSnapshot` are `readonly record struct`s.** Ticks are produced continuously across 10+ symbols — avoids a per-tick heap allocation. `PriceState`, the mutable per-symbol aggregate, stays a class.
- **Mutable aggregate + immutable snapshot.** `PriceState.Apply()` mutates in place, single writer per symbol; everything else reads a `PriceSnapshot` copied out via `ToSnapshot()` — safe for concurrent reads with no lock, since a snapshot can't be torn.
- **`TradingRules` is an immutable record, replaced as a whole on update.** Lets validation read "current rules" without locking — a single atomic reference swap.
- **`ITradingRulesEngine.Evaluate` is synchronous, no I/O.** Rules, current price, and the duplicate-id result are passed in; all I/O stays with the caller — keeps the highest-scrutiny logic trivially unit-testable.
- **Auto-generated orders reuse `Order`/`OrderDecision`.** `OrderOrigin` distinguishes them; both flow through the same rules engine, per spec.
- **No persistence yet.** `Infrastructure` compiles but registers nothing — EF Core lands Day 4.

**Day 2**
- **`PriceState` reworked to a lock-free atomic snapshot swap.** Day 1 used individually-mutated fields; now a single immutable `PriceSnapshot` is replaced via `Volatile` read/write on each tick. A reader either gets the old snapshot or the fully-formed new one, never a mix — the concurrency mechanism the Day 1 write-up deferred to "once the pricing engine exists to drive it."
- **Single consumer, not per-symbol locking.** One `BackgroundService` drains the pricing engine's merged tick stream sequentially; since ticks are processed one at a time regardless of symbol, no two ticks for the same symbol are ever applied concurrently, so `PriceState` needs no lock of its own.
- **Each simulated symbol is its own producer task with its own state.** No shared mutable data between the 10 symbol generators — only the channel they write to is shared, and channels are built for that.
- **`IPriceStateRepository.UpsertAsync(PriceSnapshot)` replaced with `ApplyTickAsync(PriceUpdate)`.** A raw "store this snapshot" method would let a caller bypass `PriceState`'s previous-price tracking; `ApplyTickAsync` is the one real write path.
- **Persistence still deferred to Day 4** — price state is in-memory only (`ConcurrentDictionary`) for now, per the Day 1 tradeoff.

**Day 3**
- **Each trading rule is its own static method on `TradingRulesEngine`.** `Evaluate` just calls all of them and collects the non-null reasons — every rule is independently unit-testable and the "which rules fired" logic isn't tangled with the checks themselves.
- **No current price → reject, don't skip.** If `currentPrice` is `null` (no tick seen yet for the symbol), the price-deviation rule rejects rather than silently passing the order through unchecked. The spec doesn't say either way; rejecting is the conservative choice for a trading system.
- **`IAutoTradingService.TryCreateOrder` takes only the latest snapshot, not a separate "previous" one.** `PriceSnapshot.PreviousMarketPrice` already carries the prior mid price (added Day 2), so a second parameter would just be redundant state the caller has to keep in sync itself.
- **Auto-order quantity: target a constant notional, not a constant quantity.** `Quantity = 10,000 / CurrentMarketPrice` keeps notional exposure comparable across instruments priced from ~0.6 (AUDUSD) to ~2000 (XAUUSD), rather than a flat quantity that would be a trivial notional on one instrument and a huge one on another.
- **Auto-trading is wired into `PriceTickProcessor` right after `ApplyTickAsync`**, using the snapshot it returns — no extra repository round-trip to re-fetch "the price that was just written."
- **Temporary in-memory `IOrderRepository` / `ITradingRulesRepository`**, same rationale as Day 2's price store: lets the full tick → auto-order → rules → persisted-decision pipeline run and be demoed before EF Core (Day 4). `ITradingRulesRepository` reuses the same lock-free swap pattern as `PriceState`.

## AI Usage Transparency

**Day 1.**
- Familiarization with the fundamentals of trading and the key determining factors.
- The 5-day execution plan (what gets built each day) was drafted with AI assistance: I gave it the task brief and the tech stack, and set the scope/priorities; AI structured that into daily milestones.

**Day 2.**
- Pricing engine, tick consumer, and the price-state concurrency design were built with AI assistance, following the Day 1 plan.

**Day 3.**
- Trading rules engine, auto-trading service, and the pipeline wiring were built with AI assistance, following the Day 2 design.

## Known Limitations

- Persistence (rules, orders, price state) is in-memory — see Design Decisions / Status; lands Day 4.
- `/debug/prices` and `/debug/orders` are temporary diagnostic endpoints, not the spec's API.
- Auto-trading's fixed 10,000 target notional, combined with the default 10,000 max-quantity rule, means low-priced instruments (e.g. AUDUSD, EURGBP) get auto-rejected on quantity more often than higher-priced ones — real behavior of the rules doing their job, but worth tuning defaults for a less lopsided demo.
- Pricing engine uses an unbounded channel — fine at 10 symbols; would need backpressure handling at a much larger scale.
- `TradingRules.SymbolWhitelist` — worth a `HashSet<string>` if the list grows large (checked on every order).

## Eventual C++ Migration

*(Filled in Day 5, once the hot paths exist and have a measured cost profile.)*

# TradingPulse — Pricing Engine & Trading Rules Service

Simulates a multi-symbol market data feed, validates/auto-generates trading orders against configurable rules, persists results, exposes a REST API. Interview take-home — see the task brief for full requirements.

**Status: Day 2 of 5.** Pricing engine (10 simulated instruments), tick consumer, in-memory latest-price store, `/health` + temporary `/debug/prices`. No trading rules, auto-trading, or persistence yet.

## How to Run

**Prerequisites:** .NET 10 SDK, Visual Studio 2022 (17.14+) or the `dotnet` CLI.

**Visual Studio:** open `TradingPulse.slnx` → `TradingPulse.Api` is already the startup project → F5.

**CLI:**
```bash
dotnet build
dotnet run --project src/TradingPulse.Api
```
Open `http://localhost:<port>/health` → `{"status":"healthy","service":"TradingPulse.Api"}`.

`GET /debug/prices` shows the pricing engine running live (10 simulated instruments, ticking every 200–600ms). Temporary — replaced by the real `/api/prices/{symbol}` on Day 4.

`dotnet test` runs the Domain unit tests (`tests/TradingPulse.Domain.Tests`).

## Structure

```
TradingPulse.slnx
├── Directory.Build.props
├── src/
│   ├── TradingPulse.Domain          — entities, value objects, enums. No dependencies.
│   ├── TradingPulse.Application     — interfaces + DTOs. Depends on Domain only.
│   ├── TradingPulse.Infrastructure  — implementations (pricing engine, tick consumer; rules/persistence Day 3–4). Depends on Application + Domain.
│   └── TradingPulse.Api             — ASP.NET Core host, composition root. Depends on all above.
└── tests/
    └── TradingPulse.Domain.Tests    — xUnit, Domain layer only so far.
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

## AI Usage Transparency

**Day 1.**
- Familiarization with the fundamentals of trading and the key determining factors.
- The 5-day execution plan (what gets built each day) was drafted with AI assistance: I gave it the task brief and the tech stack, and set the scope/priorities; AI structured that into daily milestones.

**Day 2.**
- Pricing engine, tick consumer, and the price-state concurrency design were built with AI assistance, following the Day 1 plan.

## Known Limitations

- No trading rules, auto-trading, persistence, or auth yet — see Design Decisions / Status.
- `/debug/prices` is a temporary diagnostic endpoint, not the spec's API.
- Pricing engine uses an unbounded channel — fine at 10 symbols; would need backpressure handling at a much larger scale.
- `TradingRules.SymbolWhitelist` — worth a `HashSet<string>` if the list grows large (checked on every order).

## Eventual C++ Migration

*(Filled in Day 5, once the hot paths exist and have a measured cost profile.)*

# TradingPulse — Pricing Engine & Trading Rules Service

Simulates a multi-symbol market data feed, validates/auto-generates trading orders against configurable rules, persists results, exposes a REST API. Interview take-home — see the task brief for full requirements.

**Status: Day 1 of 5.** Solution scaffold, domain model, Application interfaces, DI skeleton, `/health` endpoint. No pricing engine, rules, persistence, or real API yet.

## How to Run

**Prerequisites:** .NET 10 SDK, Visual Studio 2022 (17.14+) or the `dotnet` CLI.

**Visual Studio:** open `TradingPulse.slnx` → `TradingPulse.Api` is already the startup project → F5.

**CLI:**
```bash
dotnet build
dotnet run --project src/TradingPulse.Api
```
Open `http://localhost:<port>/health` → `{"status":"healthy","service":"TradingPulse.Api"}`.

## Structure

```
TradingPulse.slnx
├── Directory.Build.props
├── src/
│   ├── TradingPulse.Domain          — entities, value objects, enums. No dependencies.
│   ├── TradingPulse.Application     — interfaces + DTOs. Depends on Domain only.
│   ├── TradingPulse.Infrastructure  — implementations (Day 2–4). Depends on Application + Domain.
│   └── TradingPulse.Api             — ASP.NET Core host, composition root. Depends on all above.
└── tests/                           — added Day 2.
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

## AI Usage Transparency

**Day 1.**
- Familiarization with the fundamentals of trading and the key determining factors.
- The 5-day execution plan (what gets built each day) was drafted with AI assistance: I gave it the task brief and the tech stack, and set the scope/priorities; AI structured that into daily milestones.


## Known Limitations

- No persistence, tests, or auth yet — see Design Decisions / Status.
- `TradingRules.SymbolWhitelist` — worth a `HashSet<string>` if the list grows large (checked on every order).

## Eventual C++ Migration

*(Filled in Day 5, once the hot paths exist and have a measured cost profile.)*

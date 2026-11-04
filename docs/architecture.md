# Architecture

## The four dimensions

Each number in this repo is addressed by four dimensions, and each dimension lives in exactly one place:

| Dimension | Examples | Defined in | Selected with |
|---|---|---|---|
| Engine | `sqlite`, `postgres`, `sqlserver`, `mysql` | `src/DataBenchmarks.Core/Engines/` + a profile in `docker/compose.yaml` | `./bench <engine>` |
| Category | `Querying`, `Loading`, `Writes`, `Bulk`, `Concurrency` | `Categories` in `Approaches.cs` + a folder | `--scenario Querying` |
| Scenario | `Fortunes`, `SingleById`, `OrdersWithLines` | `Scenarios/<Category>/<Name>/` | `--scenario Fortunes` |
| Approach | `AdoNet`, `Dapper`, `EfCompiled`, ... | `Approaches.cs` | `--approach EfCompiled,Dapper` |

The engine is chosen per run, not per benchmark. Each run talks to one container, a missing engine never breaks a run, and the reports join engines side by side afterwards.

## Projects

```
src/DataBenchmarks.Core        Engines (IEngine, ISqlDialect), BenchContext, fixtures, seeding
src/DataBenchmarks.Scenarios   ScenarioBase, Approaches/Categories, ScenarioCatalog, Verifier, the scenarios
src/DataBenchmarks.Runner      CLI: seed | verify | run (BenchmarkDotNet) | load | list
src/DataBenchmarks.Reporting   artifacts → results/*.json, results/README.md, README summary, dashboard data
tests/...Conventions.Tests     conventions + "all approaches agree" on SQLite
```

## A run, step by step

`./bench postgres --load`:

1. `docker compose --profile postgres up -d --wait` (healthchecked).
2. `seed`: `EnsureCreated` from `BenchContext`, then each fixture fills its tables. A stamp (seeder version, dataset size, fixture list) makes this a no-op when the data is already right.
3. `run`: the **verifier** calls every approach once and compares its result with the `AdoNet` baseline as JSON. Any difference stops the run before anything is measured. Then BenchmarkDotNet runs, and each benchmark gets its own process, which reads the engine from `DATABENCH_ENGINE`.
4. `load` (with `--load`): for `[LoadTest]` scenarios, each approach is driven by 8, 32 and 128 concurrent workers (2 s warmup, 5 s measured).
5. `./bench report` / `publish`: the Reporting tool imports `artifacts/<engine>/` into `results/`. It replaces that engine's rows for the scenarios it ran, archives the run under `results/history/` and regenerates the Markdown.

## Fairness rules (enforced in `ScenarioBase`)

- One operation = one unit of work: ADO.NET and Dapper open a pooled connection per call; EF Core creates (or, for `EfPooled*`, rents) a context per call.
- One connection string per engine, so every approach gets the same ADO.NET pool.
- Ids come from a lock-free deterministic sequence. Every approach sees the same ids in the same order, and no approach gets a single hot row.
- Async throughout; EF Core logging is off (no logger is configured); nothing is cached between calls except what the approach itself is about (compiled queries, pooled contexts).

## Schema and data

`BenchContext` is the only schema definition. Mapping lives with each fixture in `Fixtures/<Name>/`, and tables and columns are lower snake_case. That way hand-written SQL in `Sql.cs` and the scenarios runs unchanged on every engine; the few non-portable bits go through `IEngine.Dialect`.

## Why benchmarks run locally, not in CI

Shared CI runners are noisy and change hardware between runs, which makes trends meaningless. Results are produced by `./bench publish` on a dedicated machine, recorded with the processor and runtime, and committed alongside the code (the pre-commit hook does this). CI's job is to prove the code builds, follows the conventions and returns correct results on every engine.

## Adding an engine

1. A class implementing `IEngine` in `Core/Engines/Engines.cs` (connection, EF provider, dialect).
2. One line in `EngineCatalog`.
3. A compose profile with a healthcheck, plus a `DATABENCH_CS_<ENGINE>` entry for the in-docker runner.

MySQL is wired this way using Oracle's `MySql.EntityFrameworkCore`, because Pomelo does not support EF Core 10 yet. It is experimental until it has been run end to end.

# DataBenchmarks

**Which data access approach is fastest in .NET, and by how much?** Repeatable [BenchmarkDotNet](https://benchmarkdotnet.org) benchmarks of Entity Framework Core configurations (tracking, no-tracking, compiled queries, context pooling, split queries, projections, raw SQL) against Dapper and hand-written ADO.NET, on SQLite, PostgreSQL and SQL Server running in Docker.

Every scenario is verified before it is measured: all approaches must return the same result, so a fast wrong answer never shows up as a win.

## Latest results

<!-- results:start -->
**Fastest single operation** per scenario and engine, and the fastest EF Core approach as a multiple of the hand-written ADO.NET baseline on the same engine (1.00× = as fast as raw ADO.NET; lower is better).

| Scenario | SQLite | PostgreSQL | SQL Server |
|---|---|---|---|
| [Loading/OrdersWithLines](results/README.md#loadingorderswithlines) | **AdoNet** 109.1 µs<br>EF Core: EfSplitQuery 3.31× | **Dapper** 374.3 µs<br>EF Core: EfProjection 2.01× | **AdoNet** 366.2 µs<br>EF Core: EfInclude 2.03× |
| [Querying/Fortunes](results/README.md#queryingfortunes) | **AdoNet** 14.3 µs<br>EF Core: EfPooledCompiled 2.47× | **AdoNet** 135.0 µs<br>EF Core: EfPooledNoTracking 1.13× | **AdoNet** 212.3 µs<br>EF Core: EfPooledCompiled 1.08× |
| [Querying/SingleById](results/README.md#queryingsinglebyid) | **AdoNet** 10.5 µs<br>EF Core: EfPooledCompiled 3.19× | **Dapper** 167.9 µs<br>EF Core: EfPooledCompiled 1.14× | **AdoNet** 201.5 µs<br>EF Core: EfPooledCompiled 1.05× |

**Highest throughput under high concurrency** (most operations per second, with p99 latency).

| Scenario | SQLite | PostgreSQL | SQL Server |
|---|---|---|---|
| Loading/OrdersWithLines | **AdoNet** 21.4k/s<br>p99 241.6 ms | **AdoNet** 9.1k/s<br>p99 50.7 ms | **AdoNet** 9.0k/s<br>p99 31.7 ms |
| Querying/Fortunes | **AdoNet** 103.1k/s<br>p99 7.8 ms | **Dapper** 24.7k/s<br>p99 12.8 ms | **Dapper** 15.1k/s<br>p99 21.6 ms |
| Querying/SingleById | **AdoNet** 208.2k/s<br>p99 3.2 ms | **AdoNet** 22.2k/s<br>p99 16.3 ms | **AdoNet** 17.0k/s<br>p99 18.0 ms |

Full tables with allocations, latency percentiles and every approach: [results/README.md](results/README.md) · Trends over time: [dashboard](https://rodmjay.github.io/DataBenchmarks/)

<sub>Last updated 2026-11-04 01:30 UTC on top of commit 270f30e · BenchmarkDotNet ShortRun job · 'standard' dataset · 11th Gen Intel Core i7-1195G7 2.90GHz · .NET 10.0.14 (10.0.14, 10.0.1426.52326)</sub>
<!-- results:end -->

## How it works

```
Engine     ×   Category   ×   Scenario          ×   Approach
postgres       Querying       Fortunes              AdoNet (baseline), Dapper, EfTracking,
sqlserver      Loading        SingleById            EfNoTracking, EfCompiled, EfPooledCompiled, ...
sqlite         Writes         OrdersWithLines
mysql          ...            ...
```

- **Engines** run in Docker ([docker/compose.yaml](docker/compose.yaml), one profile each); SQLite needs nothing.
- **Scenarios** live in `src/DataBenchmarks.Scenarios/Scenarios/<Category>/<Name>/`: a spec (`README.md`) next to one `<Name>Benchmarks` class.
- **Approaches** are methods on that class, named from one shared vocabulary ([Approaches.cs](src/DataBenchmarks.Scenarios/Approaches.cs)). `EfCompiled` means the same thing in every scenario, so results compare across scenarios.
- **Data** comes from fixtures (`src/DataBenchmarks.Core/Fixtures/`): deterministic seeds give every engine identical rows.
- **Measurements:** BenchmarkDotNet measures single-operation time and allocations. `--load` adds a concurrency run (throughput, p50/p95/p99 latency, errors) at 8, 32 and 128 concurrent callers.
- **Publishing:** `./bench publish` regenerates the section above, [results/README.md](results/README.md) and the history behind the [dashboard](https://rodmjay.github.io/DataBenchmarks/).

More detail: [docs/architecture.md](docs/architecture.md).

## Run it

Requirements: .NET 10 SDK and Docker (or only Docker, with `--in-docker`).

```bash
./bench sqlite --job dry                       # zero-setup smoke run, no containers
./bench postgres                               # start Postgres, seed, verify, benchmark (short job)
./bench sqlserver --scenario Fortunes --load   # one scenario, plus the concurrency run
./bench postgres --approach EfCompiled,Dapper  # only some approaches
./bench publish                                # all engines, then regenerate the results above
./bench list                                   # scenarios and their approaches
./bench test                                   # conventions and correctness tests
./bench down                                   # stop the containers
```

Results for one run land in `artifacts/<engine>/` (BenchmarkDotNet's Markdown, HTML, CSV and JSON reports).

## Keeping the results current

The results above are produced on a real machine, not a shared CI runner, and committed with the code that produced them:

```bash
./bench setup   # once: enables .githooks/pre-commit
```

From then on, any commit that touches `src/` or `docker/` re-runs `./bench publish` and stages the regenerated `README.md` and `results/` into the same commit. Choose engines with `DATABENCH_ENGINES="sqlite postgres"`, add the concurrency run with `DATABENCH_LOAD=1`, and skip one commit with `DATABENCH_SKIP=1 git commit ...`.

CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) builds, runs the tests and smoke-runs every engine on each push. When `results/` changes, [pages.yml](.github/workflows/pages.yml) republishes the dashboard.

## Add a scenario

See [docs/adding-a-scenario.md](docs/adding-a-scenario.md). In short:

1. Copy [docs/scenario-template.md](docs/scenario-template.md) to `Scenarios/<Category>/<Name>/README.md` and write the spec.
2. Add `<Name>Benchmarks : ScenarioBase` beside it, with one `[Benchmark]` method per approach and `AdoNet` as the baseline.
3. Run `./bench test`. The tests check that it follows the conventions and that every approach agrees.

Ideas waiting to be built: [docs/scenario-backlog.md](docs/scenario-backlog.md).

## Reading the results

See [docs/reading-results.md](docs/reading-results.md). The short version: compare **ratios to ADO.NET within one engine**, and treat absolute times as specific to the machine shown under each table.

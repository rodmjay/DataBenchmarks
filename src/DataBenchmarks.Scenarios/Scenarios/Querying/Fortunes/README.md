# Scenario: Fortunes (per-request data access overhead)

## Purpose
Measure the fixed overhead of a data access approach on a trivial read. The query
is deliberately cheap, so differences reflect the framework's own cost (context
setup, query translation and caching, materialization, connection handling)
rather than database work. Modeled on the TechEmpower "Fortunes" benchmark, which
the EF Core team used as a primary performance target.

## Data
- Table `Fortune`: `Id` (int, primary key), `Message` (string, up to 2048 chars)
- Seed with the 12 standard TechEmpower fortune rows, including the ones with
  Unicode and HTML/script characters

## Operation (one unit of work)
1. Acquire a context or connection
2. Read all rows from `Fortune`
3. Add an in-memory row: Id = 0, Message = "Additional fortune added at request time."
4. Sort by Message using ordinal comparison
5. Render as an HTML-encoded table
6. Return the result

## Approaches to compare
1. EF Core, default (new context per operation, change tracking on)
2. EF Core, no tracking
3. EF Core, pooled contexts with no tracking
4. EF Core, compiled query with no tracking
5. EF Core, pooled contexts with a compiled query and no tracking
6. Dapper
7. Raw ADO.NET (baseline)

## Measurements
- Single operation: mean time and allocations per operation, as a ratio to the baseline
- Under concurrency (low, medium, high): throughput, p50/p95/p99 latency, errors

## Rules
- Every approach must produce identical output (13 rows, same order, same encoded HTML)
- Same database, connection settings, and pool size for all approaches
- No result caching; async throughout; logging off
- Warm up before measuring

## Implementation notes
- Table and columns follow the repo's portable naming: `fortune (id, message)`.
  Data and mapping: `src/DataBenchmarks.Core/Fixtures/Fortunes/`.
- Approach mapping: 1 `EfTracking`, 2 `EfNoTracking`, 3 `EfPooledNoTracking`,
  4 `EfCompiled`, 5 `EfPooledCompiled`, 6 `Dapper`, 7 `AdoNet`.
- Steps 3 to 5 are one shared method (`FortunesHtml.Render`), so every approach pays the
  same rendering cost and only data access differs.
- Identical output is enforced by the verifier (before every run, and in the tests); concurrency runs with `--load`.

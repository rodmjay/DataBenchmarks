# Scenario: SingleById (point lookup overhead)

## Purpose
Measure the cost of the most common query in any application: fetch one row by primary key.
The database work is a single index seek, so differences come from each approach's
per-call overhead: context creation, query translation and caching, materialization.

## Data
Shop fixture (`src/DataBenchmarks.Core/Fixtures/Shop/`): `orders` table, 100,000 rows in the
standard dataset.

## Operation (one unit of work)
1. Acquire a context or connection
2. Fetch one order by a pseudo-random id (the same id sequence for every approach)
3. Return the materialized `Order`

## Approaches to compare
- `AdoNet` (baseline): hand-written SQL, DbDataReader
- `Dapper`: hand-written SQL, `QuerySingleAsync`
- `EfTracking`: `SingleAsync` with change tracking (EF Core default)
- `EfNoTracking`: `AsNoTracking().SingleAsync`
- `EfCompiled`: `EF.CompileAsyncQuery`, no tracking
- `EfPooledCompiled`: pooled context plus compiled query
- `EfRawSql`: `FromSqlInterpolated` with the same SQL as AdoNet

## Measurements
- Single operation: mean time and allocations per operation, as a ratio to the baseline
- Under concurrency (low, medium, high): throughput, p50/p95/p99 latency, errors

## Rules
- Every approach returns the same `Order` (verified before measuring)
- Same database, connection settings and pool size for all approaches
- Async throughout; logging off; no result caching

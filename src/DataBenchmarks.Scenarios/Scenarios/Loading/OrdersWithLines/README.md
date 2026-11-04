# Scenario: OrdersWithLines (one-to-many graph loading)

## Purpose
Measure how each approach loads a parent with its children. This is where the choice between
one JOIN, split queries and projection actually matters: a JOIN duplicates parent columns per
child row, split queries add a round trip, and projection skips entity materialization.

## Data
Shop fixture (`src/DataBenchmarks.Core/Fixtures/Shop/`): `orders` and `order_lines`, 3 lines
per order, about 10 orders per customer in the standard dataset.

## Operation (one unit of work)
1. Acquire a context or connection
2. Load every order of one pseudo-random customer, with its lines, ordered by order id then line id
3. Return the list of orders with lines populated

## Approaches to compare
- `AdoNet` (baseline): one JOIN, rows grouped by hand
- `Dapper`: one JOIN with multi-mapping
- `EfInclude`: `Include(o => o.Lines)`, single query
- `EfSplitQuery`: `Include` with `AsSplitQuery()`
- `EfProjection`: `Select` into the same shape

All EF approaches are no-tracking, so the comparison is about query shape only.

## Measurements
- Single operation: mean time and allocations per operation, as a ratio to the baseline
- Under concurrency (low, medium, high): throughput, p50/p95/p99 latency, errors

## Rules
- Every approach returns the same orders and lines in the same order (verified before measuring)
- Same database, connection settings and pool size for all approaches
- Async throughout; logging off; no result caching

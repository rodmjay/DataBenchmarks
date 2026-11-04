# Scenario: <Name> (<one-line summary>)

<!-- Copy to src/DataBenchmarks.Scenarios/Scenarios/<Category>/<Name>/README.md.
     This file is the spec: write it before the code, keep it in step after. -->

## Purpose
What cost this isolates, and why the result helps choose an approach.

## Data
Tables and rows used. Reuse an existing fixture (`src/DataBenchmarks.Core/Fixtures/`) or add
one under `Fixtures/<Name>/`.

## Operation (one unit of work)
1. Acquire a context or connection
2. ...
3. Return the result

## Approaches to compare
Names from `src/DataBenchmarks.Scenarios/Approaches.cs`. `AdoNet` is always the baseline.
- `AdoNet` (baseline)
- ...

## Parameters
Optional. Each becomes a BenchmarkDotNet `[Params]` property; every combination runs.

## Measurements
- Single operation: mean time and allocations per operation, as a ratio to the baseline
- Under concurrency (`[LoadTest]`): throughput, p50/p95/p99 latency, errors

## Rules
- Every approach must produce identical output (enforced by the verifier before measuring)
- Same database, connection settings, and pool size for all approaches
- Async throughout; logging off; no result caching
- Scenarios that write reset to their seeded state between iterations

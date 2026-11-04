# Reading the results

## Where to look

- **[README.md](../README.md#latest-results)**: one line per scenario. It shows the fastest approach on each engine and the fastest EF Core approach as a multiple of ADO.NET.
- **[results/README.md](../results/README.md)**: every approach, every engine, with allocations and latency percentiles under load.
- **[Dashboard](https://rodmjay.github.io/DataBenchmarks/)**: the same data as charts, plus trends across runs.
- **`results/latest.json`, `results/load.json`, `results/history/`**: the raw numbers, if you want to analyze them yourself.

## What the numbers mean

| Value | Meaning |
|---|---|
| Mean | BenchmarkDotNet's mean time for one operation (one unit of work, including connection or context acquisition). |
| Ratio (×) | Mean divided by the `AdoNet` baseline **on the same engine in the same run**. 1.00× is as fast as hand-written ADO.NET; 1.50× is 50% slower. |
| Allocated | Managed bytes allocated per operation. This is a proxy for GC pressure under load. |
| ops/s | Completed operations per second across all workers, under load. |
| p50 / p95 / p99 | Latency percentiles under load, in milliseconds. |
| errors | Failed operations under load (for example, pool exhaustion). Anything above 0 deserves a look. |

## How to compare

- **Compare ratios within a column (one engine).** Ratios cancel out the machine; absolute times don't.
- **Comparing engines?** Look at the absolute times only when the runs share a machine (see the *Runs* table in results/README.md).
- **Short jobs are indicative.** `./bench publish` uses BenchmarkDotNet's `ShortRun` to keep commits fast. For a decision that matters, re-run the scenario with `--job default` (or `-- --job long`) and read the BenchmarkDotNet error columns in `artifacts/`.
- **Under load, check p99 and errors, not just throughput.** An approach that wins on throughput but has a long tail can be the wrong choice for request-serving code.

# Adding a scenario

A scenario answers one question ("what does it cost to load a parent with its children?") by running several approaches against the same data and comparing them with the ADO.NET baseline.

## 1. Write the spec first

Copy [scenario-template.md](scenario-template.md) to:

```
src/DataBenchmarks.Scenarios/Scenarios/<Category>/<Name>/README.md
```

- **Category** is one of `Querying`, `Loading`, `Writes`, `Bulk`, `Concurrency` (see `Categories` in `Approaches.cs`). A new category is a deliberate edit to that list.
- **Name** is PascalCase, e.g. `NRandomRows`.

## 2. Pick (or add) the data

Reuse a fixture under `src/DataBenchmarks.Core/Fixtures/` if one fits. Otherwise add `Fixtures/<Name>/` with the entity classes and a class implementing `IFixture` (`Configure` for the mapping, `SeedAsync` for deterministic data), plus a `DbSet` in `BenchContext`. Fixtures are discovered automatically, and the next `./bench <engine>` reseeds.

## 3. Write the benchmark class

`<Name>Benchmarks.cs` beside the spec:

```csharp
namespace DataBenchmarks.Scenarios.Querying.MyScenario;

[Description("One sentence: what this measures. Shown in the results.")]
[LoadTest]                                   // optional: also run under concurrency
public class MyScenarioBenchmarks : ScenarioBase
{
    [Benchmark(Baseline = true), Approach(Approaches.AdoNet)]
    public async Task<Order> AdoNet() { ... }

    [Benchmark, Approach(Approaches.EfCompiled)]
    public async Task<Order> EfCompiled() { ... }
}
```

Use what `ScenarioBase` gives you: `OpenConnectionAsync()`, `NewContext()`, `RentContext()` (pooled), `NextOrderId()` / `NextCustomerId()`, `Engine.Dialect`. If several approaches share non-data-access work (like rendering in `Fortunes`), put it in one helper so every approach pays the same cost.

Parameters: add BenchmarkDotNet `[Params(1, 20, 100)] public int N { get; set; }`. Every combination runs and is reported as its own row.

## 4. Approaches come from one vocabulary

Method names must match an entry in `Approaches.cs`, and the method carries `[Approach(...)]`. If a technique is genuinely new, add it there with a one-line description. That makes it available to every scenario, and it appears in the results legend.

## 5. Check it

```bash
./bench test                                 # conventions + all approaches agree (SQLite)
./bench sqlite --scenario MyScenario --job dry
./bench postgres --scenario MyScenario
```

The tests fail, with a message saying what to fix, if:

- the class is not `*Benchmarks` in `DataBenchmarks.Scenarios.<Category>.<Name>`, or the category is unknown;
- there is no `[Description]` or no spec `README.md`;
- the baseline is missing or is not `AdoNet`;
- a method is not a registered approach, is not named after it, or is not async;
- an approach returns something different from the baseline;
- an approach is registered but no scenario uses it.

## 6. Commit

With the hook enabled (`./bench setup`), the commit re-runs the benchmarks and includes the new results in `README.md` and `results/`.

## Writes

Scenarios that change data must leave the database as seeded. Do the work inside a transaction that is rolled back, or reset in an `[IterationSetup]` (for example, truncate a table that only that scenario uses). Before measuring, the verifier compares results, so return something that proves the write happened (for example, the affected row count).

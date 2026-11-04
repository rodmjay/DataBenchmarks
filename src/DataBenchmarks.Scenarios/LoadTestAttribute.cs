namespace DataBenchmarks.Scenarios;

/// Opts a scenario into the concurrency run (`./bench <engine> --load`): each approach is called
/// by N concurrent workers for a fixed time at each concurrency level, recording throughput,
/// p50/p95/p99 latency and errors. Approaches must be thread-safe (ScenarioBase members are).
[AttributeUsage(AttributeTargets.Class)]
public sealed class LoadTestAttribute : Attribute;

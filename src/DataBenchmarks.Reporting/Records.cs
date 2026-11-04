namespace DataBenchmarks.Reporting;

/// One measured approach in one scenario on one engine. This is the unit stored in
/// results/latest.json and results/history/*.json, and the input to every report.
public sealed record ResultRecord
{
    public required string Engine { get; init; }
    public required string Category { get; init; }
    public required string Scenario { get; init; }
    public required string Approach { get; init; }
    /// BenchmarkDotNet [Params] values, e.g. "N=20"; empty for scenarios without parameters.
    public string Parameters { get; init; } = "";
    public required double MeanNs { get; init; }
    public required double StdErrNs { get; init; }
    public required long AllocatedBytes { get; init; }
    public required bool Baseline { get; init; }
    public required RunInfo Run { get; init; }

    public string ScenarioKey => Parameters.Length == 0 ? $"{Category}/{Scenario}" : $"{Category}/{Scenario} ({Parameters})";
}

/// One approach at one concurrency level, from `./bench <engine> --load`.
public sealed record LoadRecord
{
    public required string Engine { get; init; }
    public required string Category { get; init; }
    public required string Scenario { get; init; }
    public required string Approach { get; init; }
    public required string Level { get; init; }
    public required int Concurrency { get; init; }
    public required double OpsPerSecond { get; init; }
    public required double P50Ms { get; init; }
    public required double P95Ms { get; init; }
    public required double P99Ms { get; init; }
    public required long Errors { get; init; }
    public required DateTime RunAt { get; init; }
    public required string Commit { get; init; }

    public string ScenarioKey => $"{Category}/{Scenario}";
}

/// Where and how a batch of results was produced.
public sealed record RunInfo
{
    public required string Id { get; init; }
    public required DateTime RunAt { get; init; }
    public required string Commit { get; init; }
    public required string Job { get; init; }
    public required string Dataset { get; init; }
    public required string Processor { get; init; }
    public required string Runtime { get; init; }
    public required string Os { get; init; }
}

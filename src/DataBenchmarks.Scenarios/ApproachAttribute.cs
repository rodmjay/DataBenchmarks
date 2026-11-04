using BenchmarkDotNet.Attributes;

namespace DataBenchmarks.Scenarios;

/// Tags a benchmark with its approach. It is a BenchmarkDotNet category, so
/// `--approach Dapper` (or BDN's `--anyCategories Dapper`) selects it.
[AttributeUsage(AttributeTargets.Method)]
public sealed class ApproachAttribute(string approach) : BenchmarkCategoryAttribute(approach)
{
    public string Approach { get; } = approach;
}

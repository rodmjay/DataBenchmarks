using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using DataBenchmarks.Core.Engines;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Seeding;
using DataBenchmarks.Scenarios;
using Xunit;

namespace DataBenchmarks.Conventions.Tests;

/// Seeds a small SQLite database once and runs the same correctness gate that `./bench` runs
/// before measuring: every approach must return what the AdoNet baseline returns.
public sealed class SqliteFixture : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"databenchmarks-tests-{Guid.NewGuid():N}.db");

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DATABENCH_CS_SQLITE", $"Data Source={_path};Pooling=False");
        Environment.SetEnvironmentVariable(EngineCatalog.EngineVariable, "sqlite");
        await Seeder.SeedAsync(EngineCatalog.Create("sqlite"), DatasetShape.Small, TextWriter.Null);
    }

    public ValueTask DisposeAsync()
    {
        File.Delete(_path);
        return ValueTask.CompletedTask;
    }
}

public class ApproachesAgreeTests(SqliteFixture fixture) : IClassFixture<SqliteFixture>
{
    public static TheoryData<string> Scenarios => ConventionsTests.Scenarios;

    [Theory, MemberData(nameof(Scenarios))]
    public async Task All_approaches_return_the_baseline_result(string key)
    {
        _ = fixture;
        var log = new StringWriter();
        var ok = await Verifier.VerifyAsync([ScenarioCatalog.All.Single(s => s.Key == key)], log);
        Assert.True(ok, log.ToString());
    }

    [Fact]
    public async Task A_disagreeing_approach_is_caught()
    {
        var log = new StringWriter();
        var ok = await Verifier.VerifyAsync([ScenarioCatalog.Describe(typeof(BrokenBenchmarks))], log);
        Assert.False(ok);
        Assert.Contains("FAILED", log.ToString());
    }

    [Description("Deliberately wrong scenario used to prove the verifier works.")]
    public class BrokenBenchmarks : ScenarioBase
    {
        [Benchmark(Baseline = true), Approach(Approaches.AdoNet)]
        public Task<int> AdoNet() => Task.FromResult(42);

        [Benchmark, Approach(Approaches.Dapper)]
        public Task<int> Dapper() => Task.FromResult(41);
    }
}

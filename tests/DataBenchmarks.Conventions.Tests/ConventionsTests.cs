using System.Reflection;
using DataBenchmarks.Scenarios;
using Xunit;

namespace DataBenchmarks.Conventions.Tests;

/// The rules that keep the repo organized as scenarios are added. Each failure message says
/// what to change; docs/adding-a-scenario.md explains the conventions.
public class ConventionsTests
{
    public static TheoryData<string> Scenarios => [.. ScenarioCatalog.All.Select(s => s.Key)];

    private static ScenarioInfo Get(string key) => ScenarioCatalog.All.Single(s => s.Key == key);

    [Fact]
    public void Catalog_is_not_empty() => Assert.NotEmpty(ScenarioCatalog.All);

    [Theory, MemberData(nameof(Scenarios))]
    public void Lives_in_its_category_and_scenario_folder(string key)
    {
        var s = Get(key);
        Assert.True(s.Type.Name.EndsWith("Benchmarks", StringComparison.Ordinal), $"{s.Type.Name}: class name must end with 'Benchmarks'.");
        Assert.True(Categories.Descriptions.ContainsKey(s.Category),
            $"{s.Type.FullName}: '{s.Category}' is not a category. Use one of {string.Join(", ", Categories.Descriptions.Keys)} or add it to Categories in Approaches.cs.");
        Assert.Equal($"DataBenchmarks.Scenarios.{s.Category}.{s.Name}", s.Type.Namespace);
    }

    [Theory, MemberData(nameof(Scenarios))]
    public void Has_a_description_and_a_spec(string key)
    {
        var s = Get(key);
        Assert.False(string.IsNullOrWhiteSpace(s.Description), $"{s.Key}: add [Description(\"...\")] to the class (it is shown in the results).");
        var spec = Path.Combine(RepoRoot(), "src", "DataBenchmarks.Scenarios", "Scenarios", s.Category, s.Name, "README.md");
        Assert.True(File.Exists(spec), $"{s.Key}: missing spec {spec}. Copy docs/scenario-template.md.");
    }

    [Theory, MemberData(nameof(Scenarios))]
    public void Has_exactly_one_baseline_and_it_is_AdoNet(string key)
    {
        var baselines = Get(key).Approaches.Where(a => a.Baseline).Select(a => a.Name).ToList();
        Assert.True(baselines.SequenceEqual([Approaches.Baseline]),
            $"{key}: needs exactly one [Benchmark(Baseline = true)] and it must be {Approaches.Baseline}; found [{string.Join(", ", baselines)}].");
    }

    [Theory, MemberData(nameof(Scenarios))]
    public void Every_approach_is_registered_and_named_after_itself(string key)
    {
        foreach (var approach in Get(key).Approaches)
        {
            var tag = approach.Method.GetCustomAttribute<ApproachAttribute>();
            Assert.True(tag is not null, $"{key}.{approach.Method.Name}: add [Approach(Approaches.{approach.Method.Name})].");
            Assert.True(Approaches.Descriptions.ContainsKey(tag!.Approach),
                $"{key}.{approach.Method.Name}: '{tag.Approach}' is not in Approaches.cs. Reuse an existing approach or add one there with a description.");
            Assert.Equal(tag.Approach, approach.Method.Name);
        }
    }

    [Theory, MemberData(nameof(Scenarios))]
    public void Approaches_are_async(string key)
    {
        foreach (var approach in Get(key).Approaches)
        {
            Assert.True(typeof(Task).IsAssignableFrom(approach.Method.ReturnType),
                $"{key}.{approach.Name}: approaches must be async (return Task<T>), like real request code.");
        }
    }

    [Fact]
    public void Every_registered_approach_is_used_somewhere()
    {
        var used = ScenarioCatalog.All.SelectMany(s => s.Approaches).Select(a => a.Name).ToHashSet();
        var unused = Approaches.Descriptions.Keys.Where(a => !used.Contains(a)).ToList();
        Assert.True(unused.Count == 0, $"Approaches registered but not used by any scenario: {string.Join(", ", unused)}. Remove them or add a scenario that uses them.");
    }

    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DataBenchmarks.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("DataBenchmarks.slnx not found above the test output.");
    }
}

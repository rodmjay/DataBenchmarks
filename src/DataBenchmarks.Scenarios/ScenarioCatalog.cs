using System.ComponentModel;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace DataBenchmarks.Scenarios;

public sealed record ApproachInfo(string Name, MethodInfo Method, bool Baseline);

public sealed record ScenarioInfo(Type Type, string Category, string Name, string? Description, bool LoadTest, IReadOnlyList<ApproachInfo> Approaches)
{
    public string Key => $"{Category}/{Name}";
}

/// Reflection view of every scenario, shared by the runner (verify, load, list), the reports and
/// the conventions tests. A scenario is DataBenchmarks.Scenarios.<Category>.<Name>.<Name>Benchmarks.
public static class ScenarioCatalog
{
    public static IReadOnlyList<ScenarioInfo> All { get; } = typeof(ScenarioBase).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(ScenarioBase)) && !t.IsAbstract)
        .OrderBy(t => t.FullName)
        .Select(Describe)
        .ToList();

    public static ScenarioInfo Describe(Type type)
    {
        var segments = type.Namespace!.Split('.');
        var approaches = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => (Method: m, Benchmark: m.GetCustomAttribute<BenchmarkAttribute>()))
            .Where(x => x.Benchmark is not null)
            .Select(x => new ApproachInfo(x.Method.GetCustomAttribute<ApproachAttribute>()?.Approach ?? x.Method.Name, x.Method, x.Benchmark!.Baseline))
            .ToList();
        return new ScenarioInfo(
            type,
            Category: segments.Length > 2 ? segments[2] : "",
            Name: type.Name.EndsWith("Benchmarks", StringComparison.Ordinal) ? type.Name[..^"Benchmarks".Length] : type.Name,
            Description: type.GetCustomAttribute<DescriptionAttribute>()?.Description,
            LoadTest: type.GetCustomAttribute<LoadTestAttribute>() is not null,
            Approaches: approaches);
    }

    /// A ready instance: engine resolved from DATABENCH_ENGINE and GlobalSetup run.
    public static ScenarioBase Create(ScenarioInfo scenario)
    {
        var instance = (ScenarioBase)Activator.CreateInstance(scenario.Type)!;
        instance.GlobalSetup();
        return instance;
    }

    /// Calls one approach, awaiting it if it is async, and returns its result.
    public static async Task<object?> InvokeAsync(ScenarioBase instance, ApproachInfo approach)
    {
        var result = approach.Method.Invoke(instance, null);
        if (result is not Task task)
        {
            return result;
        }
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }
}

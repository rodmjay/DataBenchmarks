using System.Diagnostics;
using DataBenchmarks.Scenarios;

namespace DataBenchmarks.Runner;

public sealed record LoadResult(
    string Category, string Scenario, string Approach, string Level, int Concurrency,
    double OpsPerSecond, double P50Ms, double P95Ms, double P99Ms, long Operations, long Errors, string? FirstError);

/// Concurrency run for [LoadTest] scenarios: N workers call one approach in a closed loop for a
/// fixed time after a warmup, at each concurrency level. Every approach gets the same levels,
/// duration and connection pool.
public static class LoadRunner
{
    public static readonly (string Name, int Workers)[] DefaultLevels = [("low", 8), ("medium", 32), ("high", 128)];

    public static async Task<List<LoadResult>> RunAsync(
        IEnumerable<ScenarioInfo> scenarios, IReadOnlySet<string>? approaches, TimeSpan duration, TimeSpan warmup, TextWriter log)
    {
        // Avoid thread-pool ramp-up skewing the first level; the same for every approach.
        ThreadPool.SetMinThreads(256, 256);

        var results = new List<LoadResult>();
        foreach (var scenario in scenarios.Where(s => s.LoadTest))
        {
            foreach (var approach in scenario.Approaches.Where(a => approaches is null || approaches.Contains(a.Name)))
            {
                var instance = ScenarioCatalog.Create(scenario);
                foreach (var (level, workers) in DefaultLevels)
                {
                    await DriveAsync(instance, approach, workers, warmup);
                    var result = await DriveAsync(instance, approach, workers, duration);
                    results.Add(result with { Category = scenario.Category, Scenario = scenario.Name, Approach = approach.Name, Level = level });
                    await log.WriteLineAsync(
                        $"load {scenario.Key}.{approach.Name} {level}({workers}): {result.OpsPerSecond:F0} ops/s, " +
                        $"p50 {result.P50Ms:F2} ms, p95 {result.P95Ms:F2} ms, p99 {result.P99Ms:F2} ms, errors {result.Errors}");
                }
            }
        }
        return results;
    }

    private static async Task<LoadResult> DriveAsync(ScenarioBase instance, ApproachInfo approach, int workers, TimeSpan duration)
    {
        var latencies = new List<double>[workers];
        long errors = 0;
        string? firstError = null;
        var clock = Stopwatch.StartNew();

        await Task.WhenAll(Enumerable.Range(0, workers).Select(w => Task.Run(async () =>
        {
            var mine = latencies[w] = new List<double>(4096);
            while (clock.Elapsed < duration)
            {
                var start = Stopwatch.GetTimestamp();
                try
                {
                    await ScenarioCatalog.InvokeAsync(instance, approach);
                    mine.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref errors);
                    Interlocked.CompareExchange(ref firstError, (e.InnerException ?? e).Message, null);
                }
            }
        })));

        var elapsed = clock.Elapsed.TotalSeconds;
        var all = latencies.SelectMany(l => l).Order().ToArray();
        double Percentile(double p) => all.Length == 0 ? 0 : all[Math.Min(all.Length - 1, (int)Math.Ceiling(p * all.Length) - 1)];
        return new LoadResult("", "", approach.Name, "", workers,
            all.Length / elapsed, Percentile(0.50), Percentile(0.95), Percentile(0.99), all.Length, errors, firstError);
    }
}

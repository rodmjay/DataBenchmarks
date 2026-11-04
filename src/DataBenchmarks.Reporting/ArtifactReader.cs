using System.Text.Json;

namespace DataBenchmarks.Reporting;

/// Reads BenchmarkDotNet "full" JSON exports from artifacts/<engine>/results.
public static class ArtifactReader
{
    public static IEnumerable<ResultRecord> Read(string engineDirectory, string commit)
    {
        var meta = JsonDocument.Parse(File.ReadAllText(Path.Combine(engineDirectory, "run.json"))).RootElement;
        var engine = meta.GetProperty("engine").GetString()!;
        var startedAt = meta.GetProperty("startedAt").GetDateTime().ToUniversalTime();
        var dataset = meta.GetProperty("dataset").GetString()!;

        foreach (var file in Directory.EnumerateFiles(Path.Combine(engineDirectory, "results"), "*-report-full.json"))
        {
            var root = JsonDocument.Parse(File.ReadAllText(file)).RootElement;
            var host = root.GetProperty("HostEnvironmentInfo");
            var benchmarks = root.GetProperty("Benchmarks").EnumerateArray().ToList();
            if (benchmarks.Count == 0)
            {
                continue;
            }

            var run = new RunInfo
            {
                Id = $"{startedAt:yyyyMMdd-HHmmss}-{engine}",
                RunAt = startedAt,
                Commit = commit,
                Job = JobName(benchmarks[0].GetProperty("DisplayInfo").GetString()!),
                Dataset = dataset,
                Processor = host.GetProperty("ProcessorName").GetString()!,
                Runtime = host.GetProperty("RuntimeVersion").GetString()!,
                Os = host.GetProperty("OsVersion").GetString()!,
            };

            foreach (var b in benchmarks)
            {
                if (b.GetProperty("Statistics").ValueKind != JsonValueKind.Object)
                {
                    continue; // the benchmark failed; BenchmarkDotNet already reported it
                }
                var approach = b.GetProperty("Method").GetString()!;
                yield return new ResultRecord
                {
                    Engine = engine,
                    Category = b.GetProperty("Namespace").GetString()!.Split('.')[2],
                    Scenario = b.GetProperty("Type").GetString()!.Replace("Benchmarks", ""),
                    Approach = approach,
                    Parameters = b.GetProperty("Parameters").GetString() ?? "",
                    MeanNs = b.GetProperty("Statistics").GetProperty("Mean").GetDouble(),
                    StdErrNs = b.GetProperty("Statistics").GetProperty("StandardError").GetDouble(),
                    AllocatedBytes = b.GetProperty("Memory").GetProperty("BytesAllocatedPerOperation").GetInt64(),
                    Baseline = approach == Scenarios.Approaches.Baseline,
                    Run = run,
                };
            }
        }
    }

    public static IEnumerable<LoadRecord> ReadLoad(string engineDirectory, string commit)
    {
        var path = Path.Combine(engineDirectory, "load.json");
        if (!File.Exists(path))
        {
            yield break;
        }
        var root = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var engine = root.GetProperty("engine").GetString()!;
        var runAt = root.GetProperty("startedAt").GetDateTime().ToUniversalTime();
        foreach (var r in root.GetProperty("results").EnumerateArray())
        {
            yield return new LoadRecord
            {
                Engine = engine,
                Category = r.GetProperty("category").GetString()!,
                Scenario = r.GetProperty("scenario").GetString()!,
                Approach = r.GetProperty("approach").GetString()!,
                Level = r.GetProperty("level").GetString()!,
                Concurrency = r.GetProperty("concurrency").GetInt32(),
                OpsPerSecond = r.GetProperty("opsPerSecond").GetDouble(),
                P50Ms = r.GetProperty("p50Ms").GetDouble(),
                P95Ms = r.GetProperty("p95Ms").GetDouble(),
                P99Ms = r.GetProperty("p99Ms").GetDouble(),
                Errors = r.GetProperty("errors").GetInt64(),
                RunAt = runAt,
                Commit = commit,
            };
        }
    }

    // "SingleByIdBenchmarks.AdoNet: ShortRun(IterationCount=3, ...)" -> "ShortRun"
    private static string JobName(string displayInfo)
    {
        var job = displayInfo[(displayInfo.IndexOf(": ", StringComparison.Ordinal) + 2)..];
        var paren = job.IndexOf('(');
        return paren < 0 ? job : job[..paren];
    }
}

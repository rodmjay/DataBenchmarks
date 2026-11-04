using System.Text.Json;
using DataBenchmarks.Reporting;

// Usage (./bench report and ./bench publish call this):
//   update --artifacts artifacts --results results --readme README.md --commit <sha>
//       Imports each artifacts/<engine> run into results/latest.json and results/load.json
//       (replacing that engine's rows for the scenarios it ran), archives it under
//       results/history, and regenerates results/README.md and the summary block in README.md.
//   site --results results --out site/data/history.json
//       Flattens results/history into the single file the dashboard loads.

var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
string Option(string name, string fallback)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}
List<T> ReadList<T>(string path) => File.Exists(path) ? JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), json)! : [];

var resultsDir = Option("--results", "results");
var latestPath = Path.Combine(resultsDir, "latest.json");
var loadPath = Path.Combine(resultsDir, "load.json");
var historyDir = Path.Combine(resultsDir, "history");

switch (args.FirstOrDefault())
{
    case "update":
    {
        var artifacts = Option("--artifacts", "artifacts");
        var readmePath = Option("--readme", "README.md");
        var commit = Option("--commit", "unknown");

        var latest = ReadList<ResultRecord>(latestPath);
        var load = ReadList<LoadRecord>(loadPath);
        Directory.CreateDirectory(historyDir);

        foreach (var engineDir in Directory.Exists(artifacts) ? Directory.EnumerateDirectories(artifacts) : [])
        {
            if (File.Exists(Path.Combine(engineDir, "run.json")))
            {
                var imported = ArtifactReader.Read(engineDir, commit).ToList();
                if (imported.Count > 0 && Archive(imported[0].Run.Id, imported))
                {
                    var replaced = imported.Select(r => (r.Engine, r.ScenarioKey)).ToHashSet();
                    latest.RemoveAll(r => replaced.Contains((r.Engine, r.ScenarioKey)));
                    latest.AddRange(imported);
                    Console.WriteLine($"imported {imported.Count} benchmark results from {engineDir}");
                }
            }

            var importedLoad = ArtifactReader.ReadLoad(engineDir, commit).ToList();
            if (importedLoad.Count > 0 && Archive($"{importedLoad[0].RunAt:yyyyMMdd-HHmmss}-{importedLoad[0].Engine}-load", importedLoad))
            {
                var replaced = importedLoad.Select(r => (r.Engine, r.ScenarioKey)).ToHashSet();
                load.RemoveAll(r => replaced.Contains((r.Engine, r.ScenarioKey)));
                load.AddRange(importedLoad);
                Console.WriteLine($"imported {importedLoad.Count} load results from {engineDir}");
            }
        }

        latest = [.. latest.OrderBy(r => r.ScenarioKey).ThenBy(r => r.Engine).ThenBy(r => r.Approach)];
        load = [.. load.OrderBy(r => r.ScenarioKey).ThenBy(r => r.Engine).ThenBy(r => r.Approach).ThenBy(r => r.Concurrency)];
        File.WriteAllText(latestPath, JsonSerializer.Serialize(latest, json));
        File.WriteAllText(loadPath, JsonSerializer.Serialize(load, json));

        var report = new MarkdownReport(latest, load);
        File.WriteAllText(Path.Combine(resultsDir, "README.md"), report.Full());
        File.WriteAllText(readmePath, MarkdownReport.Splice(File.ReadAllText(readmePath), report.Summary()));
        Console.WriteLine($"wrote {resultsDir}/ and the results summary in {readmePath}");
        return 0;

        // Writes a history entry once; false if this run was already imported.
        bool Archive<T>(string id, List<T> rows)
        {
            var file = Path.Combine(historyDir, id + ".json");
            if (File.Exists(file))
            {
                Console.WriteLine($"skip: {id} already imported");
                return false;
            }
            File.WriteAllText(file, JsonSerializer.Serialize(rows, json));
            return true;
        }
    }

    case "site":
    {
        var output = Option("--out", "site/data/history.json");
        var files = Directory.Exists(historyDir) ? Directory.GetFiles(historyDir, "*.json") : [];
        var records = files.Where(f => !f.EndsWith("-load.json", StringComparison.Ordinal))
            .SelectMany(ReadList<ResultRecord>).OrderBy(r => r.Run.RunAt).ToList();
        var load = files.Where(f => f.EndsWith("-load.json", StringComparison.Ordinal))
            .SelectMany(ReadList<LoadRecord>).OrderBy(r => r.RunAt).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            generatedAt = DateTime.UtcNow,
            approaches = DataBenchmarks.Scenarios.Approaches.Descriptions,
            records,
            load,
        }, json));
        Console.WriteLine($"wrote {records.Count} benchmark and {load.Count} load records to {output}");
        return 0;
    }

    default:
        Console.Error.WriteLine("usage: (update|site) [options]; see the comment at the top of Program.cs");
        return 2;
}

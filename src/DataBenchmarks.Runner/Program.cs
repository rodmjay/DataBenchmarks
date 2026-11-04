using System.Text.Json;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using DataBenchmarks.Core.Engines;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using DataBenchmarks.Core.Seeding;
using DataBenchmarks.Runner;
using DataBenchmarks.Scenarios;

// Usage (./bench wraps these; see README.md):
//   seed   --engine <name> [--dataset small|standard]
//   verify --engine <name> [--scenario <text>]
//   run    --engine <name> [--scenario <text>] [--approach A,B] [--no-verify] [BenchmarkDotNet args, e.g. --job short]
//   load   --engine <name> [--scenario <text>] [--approach A,B] [--duration 5] [--warmup 2]
//   list
// --scenario matches part of "Category/Name", e.g. "Querying" or "Fortunes".

if (args.Length == 0)
{
    return Usage();
}

var rest = args.Skip(1).ToList();
string? Take(string option)
{
    var i = rest.IndexOf(option);
    if (i < 0 || i + 1 >= rest.Count)
    {
        return null;
    }
    var value = rest[i + 1];
    rest.RemoveRange(i, 2);
    return value;
}
bool Flag(string option) => rest.Remove(option);

var command = args[0];
if (command == "list")
{
    foreach (var s in ScenarioCatalog.All)
    {
        Console.WriteLine($"{s.Key}{(s.LoadTest ? " [load]" : "")}: {string.Join(", ", s.Approaches.Select(a => a.Baseline ? a.Name + "*" : a.Name))}");
    }
    return 0;
}

var engine = EngineCatalog.Create(Take("--engine") ?? throw new ArgumentException("--engine is required"));
// Scenario instances (verify, load) and BenchmarkDotNet child processes read the engine from here.
Environment.SetEnvironmentVariable(EngineCatalog.EngineVariable, engine.Name);

var scenarioFilter = Take("--scenario");
var approachFilter = Take("--approach")?.Split(',').ToHashSet();
var selected = ScenarioCatalog.All
    .Where(s => scenarioFilter is null || s.Key.Contains(scenarioFilter, StringComparison.OrdinalIgnoreCase))
    .ToList();
var artifacts = Path.GetFullPath(Take("--artifacts") ?? Path.Combine("artifacts", engine.Name));

switch (command)
{
    case "seed":
        await Seeder.SeedAsync(engine, DatasetShape.Parse(Take("--dataset") ?? "standard"), Console.Out);
        return 0;

    case "verify":
        return await Verifier.VerifyAsync(selected, Console.Out) ? 0 : 1;

    case "run":
    {
        if (!Flag("--no-verify") && !await Verifier.VerifyAsync(selected, Console.Out))
        {
            Console.Error.WriteLine("Approaches disagree; nothing was measured. Fix the scenario or pass --no-verify.");
            return 1;
        }

        DatasetShape shape;
        await using (var db = new BenchContext(engine.ContextOptions()))
        {
            shape = Seeder.ReadShape(db);
        }

        // Start clean so the report only ever sees this run.
        if (Directory.Exists(artifacts))
        {
            Directory.Delete(artifacts, recursive: true);
        }
        Directory.CreateDirectory(artifacts);

        if (scenarioFilter is not null)
        {
            rest.Add("--filter");
            rest.AddRange(selected.Select(s => s.Type.FullName + ".*"));
        }
        else if (!rest.Contains("--filter"))
        {
            rest.AddRange(["--filter", "*"]);
        }
        if (approachFilter is not null)
        {
            rest.Add("--anyCategories");
            rest.AddRange(approachFilter);
        }

        await WriteRunInfoAsync(Path.Combine(artifacts, "run.json"), engine.Name, shape.Name, string.Join(' ', rest));

        var config = DefaultConfig.Instance
            .WithArtifactsPath(artifacts)
            .AddExporter(JsonExporter.Full, MarkdownExporter.GitHub);
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(ScenarioBase).Assembly).Run([.. rest], config);
        return summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;
    }

    case "load":
    {
        var duration = TimeSpan.FromSeconds(double.Parse(Take("--duration") ?? "5"));
        var warmup = TimeSpan.FromSeconds(double.Parse(Take("--warmup") ?? "2"));
        var results = await LoadRunner.RunAsync(selected, approachFilter, duration, warmup, Console.Out);
        Directory.CreateDirectory(artifacts);
        await File.WriteAllTextAsync(Path.Combine(artifacts, "load.json"), JsonSerializer.Serialize(new
        {
            engine = engine.Name,
            startedAt = DateTime.UtcNow,
            durationSeconds = duration.TotalSeconds,
            results,
        }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return results.Any(r => r.Errors > 0) ? 1 : 0;
    }

    default:
        return Usage();
}

static async Task WriteRunInfoAsync(string path, string engine, string dataset, string arguments) =>
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { engine, dataset, arguments, startedAt = DateTime.UtcNow }));

static int Usage()
{
    Console.Error.WriteLine("usage: (seed|verify|run|load|list) --engine <" + string.Join('|', EngineCatalog.Names) + "> ...; see Program.cs");
    return 2;
}

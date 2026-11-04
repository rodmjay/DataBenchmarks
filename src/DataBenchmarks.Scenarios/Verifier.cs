using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataBenchmarks.Scenarios;

/// The correctness gate that runs before anything is measured: every approach in a scenario
/// must return the same result as the baseline (compared as JSON). A fast wrong answer is not
/// a result, so a mismatch fails the run.
public static class Verifier
{
    private static readonly JsonSerializerOptions Json = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

    public static async Task<bool> VerifyAsync(IEnumerable<ScenarioInfo> scenarios, TextWriter log)
    {
        var ok = true;
        foreach (var scenario in scenarios)
        {
            var baseline = scenario.Approaches.Single(a => a.Baseline);
            var expected = await ResultAsync(scenario, baseline);
            var agree = true;
            foreach (var approach in scenario.Approaches.Where(a => !a.Baseline))
            {
                var actual = await ResultAsync(scenario, approach);
                if (actual == expected)
                {
                    continue;
                }
                ok = agree = false;
                await log.WriteLineAsync($"verify FAILED {scenario.Key}.{approach.Name} differs from {baseline.Name}");
                await log.WriteLineAsync($"  expected: {Clip(expected)}");
                await log.WriteLineAsync($"  actual:   {Clip(actual)}");
            }
            if (agree)
            {
                await log.WriteLineAsync($"verify ok     {scenario.Key} ({scenario.Approaches.Count} approaches agree)");
            }
        }
        return ok;
    }

    // A fresh instance per approach, so each one sees the same id sequence.
    private static async Task<string> ResultAsync(ScenarioInfo scenario, ApproachInfo approach)
    {
        var instance = ScenarioCatalog.Create(scenario);
        return JsonSerializer.Serialize(await ScenarioCatalog.InvokeAsync(instance, approach), Json);
    }

    private static string Clip(string s) => s.Length <= 300 ? s : s[..300] + "...";
}

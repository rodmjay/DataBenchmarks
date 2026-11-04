using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Engines;

/// Every supported engine. Adding an engine = one class + one line here + one compose profile.
public static class EngineCatalog
{
    /// Environment variable the runner sets so BenchmarkDotNet child processes know their engine.
    public const string EngineVariable = "DATABENCH_ENGINE";

    public static IReadOnlyList<string> Names { get; } = ["sqlite", "postgres", "sqlserver", "mysql"];

    public static IEngine Create(string name) => name.ToLowerInvariant() switch
    {
        "sqlite" => new SqliteEngine(),
        "postgres" => new PostgresEngine(),
        "sqlserver" => new SqlServerEngine(),
        "mysql" => new MySqlEngine(),
        _ => throw new ArgumentException($"Unknown engine '{name}'. Known: {string.Join(", ", Names)}"),
    };

    public static IEngine FromEnvironment() =>
        Create(Environment.GetEnvironmentVariable(EngineVariable)
            ?? throw new InvalidOperationException($"{EngineVariable} is not set. Run through ./bench or the runner."));

    /// Connection string from DATABENCH_CS_<ENGINE>, falling back to the docker/compose.yaml defaults.
    internal static string ConnectionString(string engine, string fallback) =>
        Environment.GetEnvironmentVariable($"DATABENCH_CS_{engine.ToUpperInvariant()}") ?? fallback;

    public static DbContextOptions<BenchContext> ContextOptions(this IEngine engine)
    {
        var builder = new DbContextOptionsBuilder<BenchContext>();
        engine.Configure(builder);
        return builder.Options;
    }
}

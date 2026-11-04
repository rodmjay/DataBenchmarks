using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataBenchmarks.Core.Fixtures.Fortunes;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Scenarios.Querying.Fortunes;

[Description("TechEmpower-style Fortunes: read 12 rows, add one, sort and HTML-encode. The query is trivial, so this isolates each approach's fixed per-request overhead.")]
[LoadTest]
public class FortunesBenchmarks : ScenarioBase
{
    private const string Sql = "SELECT id, message FROM fortune";

    private static readonly Func<BenchContext, IAsyncEnumerable<Fortune>> CompiledAll =
        EF.CompileAsyncQuery((BenchContext db) => db.Fortunes.AsNoTracking());

    [Benchmark(Baseline = true), Approach(Approaches.AdoNet)]
    public async Task<string> AdoNet()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = Sql;
        var fortunes = new List<Fortune>(16);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            fortunes.Add(new Fortune { Id = reader.GetInt32(0), Message = reader.GetString(1) });
        }
        return FortunesHtml.Render(fortunes);
    }

    [Benchmark, Approach(Approaches.Dapper)]
    public async Task<string> Dapper()
    {
        await using var connection = await OpenConnectionAsync();
        var fortunes = (await connection.QueryAsync<Fortune>(Sql)).AsList();
        return FortunesHtml.Render(fortunes);
    }

    [Benchmark, Approach(Approaches.EfTracking)]
    public async Task<string> EfTracking()
    {
        await using var db = NewContext();
        return FortunesHtml.Render(await db.Fortunes.ToListAsync());
    }

    [Benchmark, Approach(Approaches.EfNoTracking)]
    public async Task<string> EfNoTracking()
    {
        await using var db = NewContext();
        return FortunesHtml.Render(await db.Fortunes.AsNoTracking().ToListAsync());
    }

    [Benchmark, Approach(Approaches.EfPooledNoTracking)]
    public async Task<string> EfPooledNoTracking()
    {
        await using var db = RentContext();
        return FortunesHtml.Render(await db.Fortunes.AsNoTracking().ToListAsync());
    }

    [Benchmark, Approach(Approaches.EfCompiled)]
    public async Task<string> EfCompiled()
    {
        await using var db = NewContext();
        return FortunesHtml.Render(await ToListAsync(CompiledAll(db)));
    }

    [Benchmark, Approach(Approaches.EfPooledCompiled)]
    public async Task<string> EfPooledCompiled()
    {
        await using var db = RentContext();
        return FortunesHtml.Render(await ToListAsync(CompiledAll(db)));
    }

    private static async Task<List<Fortune>> ToListAsync(IAsyncEnumerable<Fortune> source)
    {
        var list = new List<Fortune>(16);
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
}

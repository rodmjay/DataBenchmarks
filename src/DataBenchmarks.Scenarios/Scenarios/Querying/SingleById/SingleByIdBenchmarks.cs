using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Scenarios.Querying.SingleById;

[Description("Fetch one order by primary key: the cheapest real query, so it isolates per-call overhead on a point lookup.")]
[LoadTest]
public class SingleByIdBenchmarks : ScenarioBase
{
    private static readonly Func<BenchContext, int, Task<Order>> CompiledById =
        EF.CompileAsyncQuery((BenchContext db, int id) => db.Orders.AsNoTracking().Single(o => o.Id == id));

    [Benchmark(Baseline = true), Approach(Approaches.AdoNet)]
    public async Task<Order> AdoNet()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = Sql.OrderById;
        var id = command.CreateParameter();
        id.ParameterName = "@id";
        id.Value = NextOrderId();
        command.Parameters.Add(id);

        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new Order
        {
            Id = reader.GetInt32(0),
            CustomerId = reader.GetInt32(1),
            OrderedAt = reader.GetDateTime(2),
            Status = (OrderStatus)reader.GetInt32(3),
            Total = reader.GetDecimal(4),
        };
    }

    [Benchmark, Approach(Approaches.Dapper)]
    public async Task<Order> Dapper()
    {
        await using var connection = await OpenConnectionAsync();
        return await connection.QuerySingleAsync<Order>(Sql.OrderById, new { id = NextOrderId() });
    }

    [Benchmark, Approach(Approaches.EfTracking)]
    public async Task<Order> EfTracking()
    {
        await using var db = NewContext();
        var id = NextOrderId();
        return await db.Orders.SingleAsync(o => o.Id == id);
    }

    [Benchmark, Approach(Approaches.EfNoTracking)]
    public async Task<Order> EfNoTracking()
    {
        await using var db = NewContext();
        var id = NextOrderId();
        return await db.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
    }

    [Benchmark, Approach(Approaches.EfCompiled)]
    public async Task<Order> EfCompiled()
    {
        await using var db = NewContext();
        return await CompiledById(db, NextOrderId());
    }

    [Benchmark, Approach(Approaches.EfPooledCompiled)]
    public async Task<Order> EfPooledCompiled()
    {
        await using var db = RentContext();
        return await CompiledById(db, NextOrderId());
    }

    [Benchmark, Approach(Approaches.EfRawSql)]
    public async Task<Order> EfRawSql()
    {
        await using var db = NewContext();
        var id = NextOrderId();
        var rows = await db.Orders.FromSqlInterpolated($"SELECT id, customer_id, ordered_at, status, total FROM orders WHERE id = {id}")
            .AsNoTracking()
            .ToListAsync();
        return rows.Single();
    }
}

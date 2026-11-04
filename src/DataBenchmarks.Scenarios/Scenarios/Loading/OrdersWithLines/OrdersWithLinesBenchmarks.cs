using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Scenarios.Loading.OrdersWithLines;

[Description("Load every order of one customer together with its lines: a one-to-many graph, where the choice between a single JOIN, split queries and projection actually matters. All EF approaches are no-tracking so the comparison is about query shape only.")]
[LoadTest]
public class OrdersWithLinesBenchmarks : ScenarioBase
{
    [Benchmark(Baseline = true), Approach(Approaches.AdoNet)]
    public async Task<List<Order>> AdoNet()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = Sql.OrdersWithLinesByCustomer;
        var customerId = command.CreateParameter();
        customerId.ParameterName = "@customerId";
        customerId.Value = NextCustomerId();
        command.Parameters.Add(customerId);

        var orders = new List<Order>();
        Order? current = null;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var orderId = reader.GetInt32(0);
            if (current?.Id != orderId)
            {
                current = new Order
                {
                    Id = orderId,
                    CustomerId = reader.GetInt32(1),
                    OrderedAt = reader.GetDateTime(2),
                    Status = (OrderStatus)reader.GetInt32(3),
                    Total = reader.GetDecimal(4),
                };
                orders.Add(current);
            }
            current.Lines.Add(new OrderLine
            {
                Id = reader.GetInt32(5),
                OrderId = reader.GetInt32(6),
                ProductId = reader.GetInt32(7),
                Quantity = reader.GetInt32(8),
                UnitPrice = reader.GetDecimal(9),
            });
        }
        return orders;
    }

    [Benchmark, Approach(Approaches.Dapper)]
    public async Task<List<Order>> Dapper()
    {
        await using var connection = await OpenConnectionAsync();
        var orders = new Dictionary<int, Order>();
        await connection.QueryAsync<Order, OrderLine, Order>(
            Sql.OrdersWithLinesByCustomer,
            (order, line) =>
            {
                if (!orders.TryGetValue(order.Id, out var existing))
                {
                    orders.Add(order.Id, existing = order);
                }
                existing.Lines.Add(line);
                return existing;
            },
            new { customerId = NextCustomerId() },
            splitOn: "id");
        return [.. orders.Values];
    }

    [Benchmark, Approach(Approaches.EfInclude)]
    public async Task<List<Order>> EfInclude()
    {
        await using var db = NewContext();
        var customerId = NextCustomerId();
        return await db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.Id)
            .ToListAsync();
    }

    [Benchmark, Approach(Approaches.EfSplitQuery)]
    public async Task<List<Order>> EfSplitQuery()
    {
        await using var db = NewContext();
        var customerId = NextCustomerId();
        return await db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .AsSplitQuery()
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.Id)
            .ToListAsync();
    }

    [Benchmark, Approach(Approaches.EfProjection)]
    public async Task<List<Order>> EfProjection()
    {
        await using var db = NewContext();
        var customerId = NextCustomerId();
        return await db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.Id)
            .Select(o => new Order
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                OrderedAt = o.OrderedAt,
                Status = o.Status,
                Total = o.Total,
                Lines = o.Lines.OrderBy(l => l.Id).Select(l => new OrderLine
                {
                    Id = l.Id,
                    OrderId = l.OrderId,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                }).ToList(),
            })
            .ToListAsync();
    }
}

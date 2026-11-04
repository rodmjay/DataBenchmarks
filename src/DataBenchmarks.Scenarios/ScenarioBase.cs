using System.Data.Common;
using BenchmarkDotNet.Attributes;
using DataBenchmarks.Core.Engines;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using DataBenchmarks.Core.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataBenchmarks.Scenarios;

/// Base class for every scenario. It owns the plumbing so scenarios only contain approaches:
/// the engine (from DATABENCH_ENGINE), connection and context factories, the seeded dataset
/// shape and a deterministic id sequence.
///
/// Fairness rules: every operation is one unit of work, like a web request. ADO.NET and Dapper
/// open a pooled connection per operation; EF Core creates (or rents) a context per operation.
/// All approaches share one connection string, so they share the same ADO.NET pool settings.
/// Members are safe to call concurrently, so the same methods also run under `load`.
[MemoryDiagnoser]
public abstract class ScenarioBase
{
    private DbContextOptions<BenchContext> _options = null!;
    private PooledDbContextFactory<BenchContext> _pool = null!;
    private int _cursor;

    static ScenarioBase() => Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    protected IEngine Engine { get; private set; } = null!;

    protected DatasetShape Dataset { get; private set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Engine = EngineCatalog.FromEnvironment();
        _options = Engine.ContextOptions();
        _pool = new PooledDbContextFactory<BenchContext>(_options);
        using (var db = NewContext())
        {
            Dataset = Seeder.ReadShape(db);
        }
        Setup();
    }

    /// Optional per-scenario setup that runs once, after the engine is resolved.
    protected virtual void Setup()
    {
    }

    /// A new, unpooled context (EF Core default usage).
    protected BenchContext NewContext() => new(_options);

    /// A context rented from an EF Core context pool; dispose returns it to the pool.
    protected BenchContext RentContext() => _pool.CreateDbContext();

    protected DbConnection OpenConnection()
    {
        var connection = Engine.CreateConnection();
        connection.Open();
        return connection;
    }

    protected async Task<DbConnection> OpenConnectionAsync()
    {
        var connection = Engine.CreateConnection();
        await connection.OpenAsync();
        return connection;
    }

    /// Ids walk the seeded range in the same pseudo-random order for every approach and engine,
    /// so caches are exercised equally and no approach gets a hot single row.
    protected int NextOrderId() => Next(Dataset.Orders);

    protected int NextCustomerId() => Next(Dataset.Customers);

    private int Next(int max)
    {
        // Weyl sequence: lock-free, deterministic from a fresh instance, and scattered across the range.
        var step = (uint)Interlocked.Add(ref _cursor, unchecked((int)0x9E37_79B9));
        return (int)(step % (uint)max) + 1;
    }
}

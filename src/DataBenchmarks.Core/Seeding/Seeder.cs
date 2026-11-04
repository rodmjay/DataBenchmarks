using DataBenchmarks.Core.Engines;
using DataBenchmarks.Core.Fixtures;
using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Seeding;

/// Creates the schema from BenchContext and runs every fixture. Idempotent: an engine that
/// already holds the same version, dataset shape and fixture set is left alone.
public static class Seeder
{
    /// Bump when a fixture's data or mapping changes so existing databases are reseeded.
    public const string Version = "2";

    public const int ChunkSize = 5_000;

    public static async Task SeedAsync(IEngine engine, DatasetShape shape, TextWriter log)
    {
        var options = engine.ContextOptions();
        var fixtures = string.Join(',', IFixture.All.Select(f => f.GetType().Name));
        var stamp = $"{Version}:{shape.Name}:{fixtures}";

        await using (var db = new BenchContext(options))
        {
            if (await CurrentStampAsync(db) == stamp)
            {
                await log.WriteLineAsync($"[{engine.Name}] dataset '{shape.Name}' already present ({stamp})");
                return;
            }

            await log.WriteLineAsync($"[{engine.Name}] creating schema");
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        var started = DateTime.UtcNow;
        foreach (var fixture in IFixture.All)
        {
            await fixture.SeedAsync(options, shape);
        }

        await InsertAsync(options, new[]
        {
            new DatasetInfo { Key = "stamp", Value = stamp },
            new DatasetInfo { Key = "dataset", Value = shape.Name },
            new DatasetInfo { Key = "customers", Value = shape.Customers.ToString() },
            new DatasetInfo { Key = "products", Value = shape.Products.ToString() },
            new DatasetInfo { Key = "orders", Value = shape.Orders.ToString() },
        });

        await log.WriteLineAsync($"[{engine.Name}] seeded '{shape.Name}' ({fixtures}) in {(DateTime.UtcNow - started).TotalSeconds:F1}s");
    }

    /// Reads the shape that is actually loaded, so scenarios pick ids inside the seeded range.
    public static DatasetShape ReadShape(BenchContext db)
    {
        var values = db.DatasetInfo.AsNoTracking().ToDictionary(x => x.Key, x => x.Value);
        if (!values.TryGetValue("dataset", out var name))
        {
            throw new InvalidOperationException("Database is not seeded. Run: ./bench <engine> (or the runner's `seed` command).");
        }
        return new DatasetShape(name, int.Parse(values["customers"]), int.Parse(values["products"]), int.Parse(values["orders"]));
    }

    /// Inserts rows in chunks, one short-lived context per chunk.
    public static async Task InsertAsync<T>(DbContextOptions<BenchContext> options, IEnumerable<T> rows) where T : class
    {
        foreach (var chunk in rows.Chunk(ChunkSize))
        {
            await using var db = new BenchContext(options);
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.Set<T>().AddRange(chunk);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<string?> CurrentStampAsync(BenchContext db)
    {
        try
        {
            return await db.DatasetInfo.Where(x => x.Key == "stamp").Select(x => x.Value).FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            return null; // database or table does not exist yet
        }
    }
}

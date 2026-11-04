using DataBenchmarks.Core.Fixtures.Shop;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Fixtures;

/// A self-contained piece of test data: its tables (EF mapping) and how to fill them.
/// Fixtures live in Fixtures/<Name>/ and are discovered by reflection, so adding one needs
/// no registration. Every fixture is created and seeded on every engine.
///
/// Mapping rule: tables and columns are lower snake_case so hand-written SQL (ADO.NET, Dapper,
/// EF raw SQL) is identical on every engine without quoting.
public interface IFixture
{
    void Configure(ModelBuilder model);

    Task SeedAsync(DbContextOptions<BenchContext> options, DatasetShape shape);

    static IReadOnlyList<IFixture> All { get; } = typeof(IFixture).Assembly.GetTypes()
        .Where(t => typeof(IFixture).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
        .OrderBy(t => t.FullName)
        .Select(t => (IFixture)Activator.CreateInstance(t)!)
        .ToList();
}

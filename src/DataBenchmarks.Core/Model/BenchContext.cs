using DataBenchmarks.Core.Fixtures;
using DataBenchmarks.Core.Fixtures.Fortunes;
using DataBenchmarks.Core.Fixtures.Shop;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Model;

/// The single context shared by every engine and approach. Mapping lives with each fixture
/// (Fixtures/<Name>/); this class only exposes the sets.
public class BenchContext(DbContextOptions<BenchContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Fortune> Fortunes => Set<Fortune>();
    public DbSet<DatasetInfo> DatasetInfo => Set<DatasetInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatasetInfo>(e =>
        {
            e.ToTable("dataset_info");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasColumnName("key").HasMaxLength(64);
            e.Property(x => x.Value).HasColumnName("value").HasMaxLength(256);
        });

        foreach (var fixture in IFixture.All)
        {
            fixture.Configure(modelBuilder);
        }
    }
}

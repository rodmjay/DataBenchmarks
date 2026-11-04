using DataBenchmarks.Core.Model;
using DataBenchmarks.Core.Seeding;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Fixtures.Fortunes;

/// The 12 standard TechEmpower "Fortunes" rows, including the Unicode and HTML/script ones
/// that exercise encoding. Independent of the dataset size.
public sealed class FortunesFixture : IFixture
{
    public static IReadOnlyList<Fortune> Rows { get; } =
    [
        new() { Id = 1, Message = "fortune: No such file or directory" },
        new() { Id = 2, Message = "A computer scientist is someone who fixes things that aren't broken." },
        new() { Id = 3, Message = "After enough decimal places, nobody gives a damn." },
        new() { Id = 4, Message = "A bad random number generator: 1, 1, 1, 1, 1, 4.33e+67, 1, 1, 1" },
        new() { Id = 5, Message = "A computer program does what you tell it to do, not what you want it to do." },
        new() { Id = 6, Message = "Emacs is a nice operating system, but I prefer UNIX. — Tom Christaensen" },
        new() { Id = 7, Message = "Any program that runs right is obsolete." },
        new() { Id = 8, Message = "A list is only as strong as its weakest link. — Donald Knuth" },
        new() { Id = 9, Message = "Feature: A bug with seniority." },
        new() { Id = 10, Message = "Computers make very fast, very accurate mistakes." },
        new() { Id = 11, Message = "<script>alert(\"This should not be displayed in a browser alert box.\");</script>" },
        new() { Id = 12, Message = "フレームワークのベンチマーク" },
    ];

    public void Configure(ModelBuilder b) => b.Entity<Fortune>(e =>
    {
        e.ToTable("fortune");
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Message).HasColumnName("message").HasMaxLength(2048);
    });

    public Task SeedAsync(DbContextOptions<BenchContext> options, Shop.DatasetShape shape) =>
        Seeder.InsertAsync(options, Rows.Select(r => new Fortune { Id = r.Id, Message = r.Message }));
}

using Bogus;
using DataBenchmarks.Core.Model;
using DataBenchmarks.Core.Seeding;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Fixtures.Shop;

/// Deterministic shop data: the same seed produces the same rows on every engine.
public sealed class ShopFixture : IFixture
{
    private const int Seed = 20261103;
    private static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.City).HasDatabaseName("ix_customers_city");
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(32);
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.OrderedAt).HasColumnName("ordered_at");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
            e.HasOne(x => x.Customer).WithMany(c => c.Orders).HasForeignKey(x => x.CustomerId);
            e.HasIndex(x => x.CustomerId).HasDatabaseName("ix_orders_customer_id");
        });

        b.Entity<OrderLine>(e =>
        {
            e.ToTable("order_lines");
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            e.HasOne<Order>().WithMany(o => o.Lines).HasForeignKey(x => x.OrderId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasIndex(x => x.OrderId).HasDatabaseName("ix_order_lines_order_id");
        });
    }

    public async Task SeedAsync(DbContextOptions<BenchContext> options, DatasetShape shape)
    {
        var faker = new Faker { Random = new Randomizer(Seed) };

        var products = Enumerable.Range(1, shape.Products).Select(id => new Product
        {
            Id = id,
            Sku = $"SKU-{id:D6}",
            Name = faker.Commerce.ProductName(),
            Price = Math.Round(faker.Random.Decimal(1, 500), 2),
        }).ToList();
        await Seeder.InsertAsync(options, products);

        await Seeder.InsertAsync(options, Enumerable.Range(1, shape.Customers).Select(id => new Customer
        {
            Id = id,
            Name = faker.Name.FullName(),
            Email = $"customer{id}@example.com",
            City = faker.Address.City(),
            CreatedAt = Epoch.AddMinutes(faker.Random.Int(0, 525_600)),
        }));

        var orders = new List<Order>(Seeder.ChunkSize);
        var lines = new List<OrderLine>(Seeder.ChunkSize * DatasetShape.LinesPerOrder);
        for (var id = 1; id <= shape.Orders; id++)
        {
            var order = new Order
            {
                Id = id,
                CustomerId = faker.Random.Int(1, shape.Customers),
                OrderedAt = Epoch.AddMinutes(faker.Random.Int(0, 525_600)),
                Status = faker.PickRandom<OrderStatus>(),
            };
            for (var n = 0; n < DatasetShape.LinesPerOrder; n++)
            {
                var product = products[faker.Random.Int(0, products.Count - 1)];
                var line = new OrderLine
                {
                    Id = (id - 1) * DatasetShape.LinesPerOrder + n + 1,
                    OrderId = id,
                    ProductId = product.Id,
                    Quantity = faker.Random.Int(1, 10),
                    UnitPrice = product.Price,
                };
                order.Total += line.Quantity * line.UnitPrice;
                lines.Add(line);
            }
            orders.Add(order);

            if (orders.Count == Seeder.ChunkSize || id == shape.Orders)
            {
                await Seeder.InsertAsync(options, orders);
                await Seeder.InsertAsync(options, lines);
                orders.Clear();
                lines.Clear();
            }
        }
    }
}

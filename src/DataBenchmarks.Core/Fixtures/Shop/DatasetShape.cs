namespace DataBenchmarks.Core.Fixtures.Shop;

/// Size of the seeded dataset. Every engine gets byte-for-byte the same rows for a given shape.
public sealed record DatasetShape(string Name, int Customers, int Products, int Orders)
{
    public const int LinesPerOrder = 3;

    public int OrderLines => Orders * LinesPerOrder;

    public static DatasetShape Small { get; } = new("small", 1_000, 100, 5_000);
    public static DatasetShape Standard { get; } = new("standard", 10_000, 1_000, 100_000);

    public static DatasetShape Parse(string name) => name.ToLowerInvariant() switch
    {
        "small" => Small,
        "standard" => Standard,
        _ => throw new ArgumentException($"Unknown dataset '{name}'. Known: small, standard"),
    };
}

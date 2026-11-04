namespace DataBenchmarks.Scenarios;

/// The allowlist of approaches. A [Benchmark] method must be named exactly after one of these
/// and carry the matching [Approach]; the conventions tests enforce it. Adding an approach is a
/// deliberate, reviewed change to this file, not something that appears ad hoc in a scenario.
public static class Approaches
{
    public const string AdoNet = nameof(AdoNet);
    public const string Dapper = nameof(Dapper);
    public const string EfTracking = nameof(EfTracking);
    public const string EfNoTracking = nameof(EfNoTracking);
    public const string EfCompiled = nameof(EfCompiled);
    public const string EfPooledNoTracking = nameof(EfPooledNoTracking);
    public const string EfPooledCompiled = nameof(EfPooledCompiled);
    public const string EfRawSql = nameof(EfRawSql);
    public const string EfInclude = nameof(EfInclude);
    public const string EfSplitQuery = nameof(EfSplitQuery);
    public const string EfProjection = nameof(EfProjection);

    /// Every scenario must include this approach as its baseline (ratios are relative to it).
    public const string Baseline = AdoNet;

    public static IReadOnlyDictionary<string, string> Descriptions { get; } = new Dictionary<string, string>
    {
        [AdoNet] = "Hand-written SQL and a DbDataReader. The floor every other approach is measured against.",
        [Dapper] = "Hand-written SQL mapped by Dapper.",
        [EfTracking] = "LINQ with change tracking (EF Core default).",
        [EfNoTracking] = "LINQ with AsNoTracking().",
        [EfCompiled] = "A no-tracking query precompiled once with EF.CompileQuery / CompileAsyncQuery.",
        [EfPooledNoTracking] = "Context rented from an EF Core context pool (DbContext pooling), no tracking.",
        [EfPooledCompiled] = "Pooled context plus a precompiled no-tracking query: EF Core's fastest configuration.",
        [EfRawSql] = "EF Core materializing hand-written SQL via FromSql.",
        [EfInclude] = "Related data loaded with Include() in a single query.",
        [EfSplitQuery] = "Include() with AsSplitQuery(): one query per collection.",
        [EfProjection] = "Select() into exactly the shape needed; no entity materialization.",
    };
}

/// The allowlist of categories. A scenario lives in Scenarios/<Category>/ with namespace
/// DataBenchmarks.Scenarios.<Category>.
public static class Categories
{
    public static IReadOnlyDictionary<string, string> Descriptions { get; } = new Dictionary<string, string>
    {
        ["Querying"] = "Reading single entities or flat lists.",
        ["Loading"] = "Reading object graphs: related data, joins, includes.",
        ["Writes"] = "Insert, update and delete of individual entities.",
        ["Bulk"] = "Set-based and high-volume writes.",
        ["Concurrency"] = "Parallel load against a shared database.",
    };
}

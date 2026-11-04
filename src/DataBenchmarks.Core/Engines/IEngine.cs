using System.Data.Common;
using DataBenchmarks.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace DataBenchmarks.Core.Engines;

/// One database engine: how to connect with ADO.NET and how to configure EF Core.
public interface IEngine
{
    /// Short id used on the command line, in compose profiles and in results (e.g. "postgres").
    string Name { get; }

    string ConnectionString { get; }

    DbConnection CreateConnection();

    void Configure(DbContextOptionsBuilder<BenchContext> options);

    /// Engine-specific SQL fragments, so raw SQL approaches are written once.
    ISqlDialect Dialect { get; }
}

public interface ISqlDialect
{
    /// Appends a row limit to a query that already has an ORDER BY.
    string Limit(string sql, int rows);
}

public sealed class OffsetFetchDialect : ISqlDialect
{
    public string Limit(string sql, int rows) => $"{sql} OFFSET 0 ROWS FETCH NEXT {rows} ROWS ONLY";
}

public sealed class LimitDialect : ISqlDialect
{
    public string Limit(string sql, int rows) => $"{sql} LIMIT {rows}";
}

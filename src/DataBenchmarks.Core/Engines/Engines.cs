using System.Data.Common;
using DataBenchmarks.Core.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace DataBenchmarks.Core.Engines;

// Default connection strings match docker/compose.yaml (host ports are offset to avoid clashes).

public sealed class SqliteEngine : IEngine
{
    public string Name => "sqlite";
    public string ConnectionString { get; } = EngineCatalog.ConnectionString("sqlite",
        $"Data Source={Path.Combine(Path.GetTempPath(), "databenchmarks.db")};Pooling=True");
    public ISqlDialect Dialect { get; } = new LimitDialect();
    public DbConnection CreateConnection() => new SqliteConnection(ConnectionString);
    public void Configure(DbContextOptionsBuilder<BenchContext> options) => options.UseSqlite(ConnectionString);
}

public sealed class PostgresEngine : IEngine
{
    public string Name => "postgres";
    public string ConnectionString { get; } = EngineCatalog.ConnectionString("postgres",
        "Host=localhost;Port=15432;Database=bench;Username=bench;Password=bench");
    public ISqlDialect Dialect { get; } = new LimitDialect();
    public DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);
    public void Configure(DbContextOptionsBuilder<BenchContext> options) => options.UseNpgsql(ConnectionString);
}

public sealed class SqlServerEngine : IEngine
{
    public string Name => "sqlserver";
    public string ConnectionString { get; } = EngineCatalog.ConnectionString("sqlserver",
        "Server=localhost,11433;Database=bench;User Id=sa;Password=Bench_Passw0rd!;TrustServerCertificate=True");
    public ISqlDialect Dialect { get; } = new OffsetFetchDialect();
    public DbConnection CreateConnection() => new SqlConnection(ConnectionString);
    public void Configure(DbContextOptionsBuilder<BenchContext> options) => options.UseSqlServer(ConnectionString);
}

/// Uses Oracle's provider: Pomelo does not support EF Core 10 yet.
public sealed class MySqlEngine : IEngine
{
    public string Name => "mysql";
    public string ConnectionString { get; } = EngineCatalog.ConnectionString("mysql",
        "Server=localhost;Port=13306;Database=bench;User=root;Password=bench");
    public ISqlDialect Dialect { get; } = new LimitDialect();
    public DbConnection CreateConnection() => new MySqlConnection(ConnectionString);
    public void Configure(DbContextOptionsBuilder<BenchContext> options) => options.UseMySQL(ConnectionString);
}

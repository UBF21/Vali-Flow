using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vali_Flow.Tests.Infrastructure;

/// <summary>
/// Wraps a TestDbContext backed by an open SQLite in-memory connection.
/// The connection is held open so that EFCore.BulkExtensions can reuse it
/// (each new ":memory:" connection would otherwise get a different, empty database).
/// Dispose this wrapper to close both the context and the connection.
/// </summary>
public sealed class SqliteTestContext : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;
    public TestDbContext Context { get; }

    private SqliteTestContext(SqliteConnection connection, TestDbContext ctx)
    {
        _connection = connection;
        Context = ctx;
    }

    /// <summary>Creates a new SQLite in-memory context with schema already applied.</summary>
    public static SqliteTestContext Create()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new TestDbContext(opts);
        ctx.Database.EnsureCreated();
        return new SqliteTestContext(conn, ctx);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

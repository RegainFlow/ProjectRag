using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectRag.Infrastructure;

namespace ProjectRag.Tests.Support;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
    }

    public RagDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseSqlite(_connection)
            .Options;

        var db = new RagDbContext(options);
        db.Database.EnsureCreated();

        return db;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

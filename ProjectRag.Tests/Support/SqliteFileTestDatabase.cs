using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectRag.Infrastructure;

namespace ProjectRag.Tests.Support;

public sealed class SqliteFileTestDatabase : IDisposable
{
    private readonly string _databasePath;

    public SqliteFileTestDatabase()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"projectrag-tests-{Guid.NewGuid():N}.sqlite");
    }

    public string ConnectionString => $"Data Source={_databasePath};";

    public RagDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseSqlite(ConnectionString)
            .Options;

        var db = new RagDbContext(options);
        db.Database.EnsureCreated();

        return db;
    }
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

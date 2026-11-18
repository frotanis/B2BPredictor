using B2BPredictor.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.UnitTests.TestHelpers;

public sealed class SqliteTestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public B2BPredictorDbContext Context { get; }

    public SqliteTestDb()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<B2BPredictorDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new B2BPredictorDbContext(options);
        Context.Database.EnsureCreated();
    }

    public B2BPredictorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<B2BPredictorDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new B2BPredictorDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

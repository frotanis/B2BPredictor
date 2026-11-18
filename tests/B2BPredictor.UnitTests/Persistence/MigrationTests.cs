using B2BPredictor.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.UnitTests.Persistence;

public sealed class MigrationTests
{
    [Fact]
    public async Task InitialMigrationCreatesAllTables()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<B2BPredictorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new B2BPredictorDbContext(options);
        await context.Database.MigrateAsync();

        var tables = new List<string>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("AppUsers", tables);
        Assert.Contains("Clients", tables);
        Assert.Contains("Orders", tables);
        Assert.Contains("OrderItems", tables);
        Assert.Contains("PredictionHistory", tables);
        Assert.Contains("PredictionRiskFactors", tables);
    }
}

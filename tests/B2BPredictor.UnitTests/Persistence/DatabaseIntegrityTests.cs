using B2BPredictor.Domain.Entities;
using B2BPredictor.Domain.Enums;
using B2BPredictor.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.UnitTests.Persistence;

public sealed class DatabaseIntegrityTests
{
    [Fact]
    public async Task DuplicateUserEmailIsRejected()
    {
        using var database = new SqliteTestDb();

        database.Context.Users.AddRange(
            CreateUser("first@test.local"),
            CreateUser("first@test.local"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateClientEmailIsRejected()
    {
        using var database = new SqliteTestDb();

        database.Context.Clients.AddRange(
            CreateClient("First Client", "first@client.local"),
            CreateClient("Second Client", "first@client.local"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task OrderWithoutClientIsRejected()
    {
        using var database = new SqliteTestDb();

        database.Context.Orders.Add(CreateOrder(Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task NegativeOrderAmountIsRejected()
    {
        using var database = new SqliteTestDb();
        var client = CreateClient("Client", "client@test.local");
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var order = CreateOrder(client.Id);
        order.Amount = -1m;
        database.Context.Orders.Add(order);

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task InvalidClientCreditRatingIsRejected()
    {
        using var database = new SqliteTestDb();
        var client = CreateClient("Client", "client@test.local");
        client.CreditRating = 0;
        database.Context.Clients.Add(client);

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingClientWithOrdersIsRestricted()
    {
        using var database = new SqliteTestDb();
        var client = CreateClient("Client", "client@test.local");
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        database.Context.Orders.Add(CreateOrder(client.Id));
        await database.Context.SaveChangesAsync();

        using var deleteContext = database.CreateContext();
        var clientToDelete = await deleteContext.Clients.SingleAsync(candidate => candidate.Id == client.Id);
        deleteContext.Clients.Remove(clientToDelete);

        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingOrderCascadesToItemsAndPredictions()
    {
        using var database = new SqliteTestDb();
        var client = CreateClient("Client", "client@test.local");
        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();

        var order = CreateOrder(client.Id);
        order.Items.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductName = "Product",
            Quantity = 2,
            UnitPrice = 100m
        });

        var prediction = new PredictionHistory
        {
            Id = Guid.NewGuid(),
            SuccessProbability = 70,
            IsLikelySuccessful = true,
            RiskLevel = "Low",
            ModelVersion = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        prediction.RiskFactors.Add(new PredictionRiskFactor
        {
            Id = Guid.NewGuid(),
            Description = "Risk"
        });

        order.Predictions.Add(prediction);
        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();

        database.Context.Orders.Remove(order);
        await database.Context.SaveChangesAsync();

        Assert.Equal(0, await database.Context.OrderItems.CountAsync());
        Assert.Equal(0, await database.Context.Predictions.CountAsync());
        Assert.Equal(0, await database.Context.PredictionRiskFactors.CountAsync());
    }

    [Fact]
    public async Task ExpectedIndexesExist()
    {
        using var database = new SqliteTestDb();
        var connection = database.Context.Database.GetDbConnection();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index'";

        var indexNames = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexNames.Add(reader.GetString(0));
        }

        Assert.Contains("IX_AppUsers_Email", indexNames);
        Assert.Contains("IX_Clients_ContactEmail", indexNames);
        Assert.Contains("IX_Orders_ClientId", indexNames);
        Assert.Contains("IX_Orders_Status", indexNames);
        Assert.Contains("IX_Orders_ClientId_Status", indexNames);
        Assert.Contains("IX_PredictionHistory_OrderId", indexNames);
        Assert.Contains("IX_PredictionRiskFactors_PredictionHistoryId", indexNames);
    }

    private static AppUser CreateUser(string email)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = email,
            PasswordHash = "hash",
            Role = UserRole.Manager,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Client CreateClient(string name, string email)
    {
        return new Client
        {
            Id = Guid.NewGuid(),
            Name = name,
            Industry = "IT",
            ContactEmail = email,
            CreditRating = 50,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Order CreateOrder(Guid clientId)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Amount = 1000m,
            PaymentTermsDays = 30,
            ItemsCount = 1,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

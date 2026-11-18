using B2BPredictor.Application.Contracts;
using B2BPredictor.Domain.Entities;
using B2BPredictor.Infrastructure.Services;
using B2BPredictor.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using DomainOrderStatus = B2BPredictor.Domain.Enums.OrderStatus;

namespace B2BPredictor.UnitTests.Services;

public sealed class ClientServiceTests
{
    [Fact]
    public async Task CreateAsyncPersistsNormalizedClient()
    {
        using var database = new SqliteTestDb();
        var service = new ClientService(database.Context);

        var result = await service.CreateAsync(new CreateClientRequest(
            "  Acme Trading  ",
            "  Retail  ",
            "  Sales@Acme.Local  ",
            75));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Acme Trading", result.Value.Name);
        Assert.Equal("Retail", result.Value.Industry);
        Assert.Equal("sales@acme.local", result.Value.ContactEmail);
        Assert.Equal(75, result.Value.CreditRating);
        Assert.Equal(0, result.Value.TotalOrdersCount);

        var client = await database.Context.Clients.SingleAsync();
        Assert.Equal("sales@acme.local", client.ContactEmail);
    }

    [Fact]
    public async Task CreateAsyncRejectsDuplicateEmail()
    {
        using var database = new SqliteTestDb();
        await AddClientAsync(database, "Existing", "IT", "client@test.local", 70);
        var service = new ClientService(database.Context);

        var result = await service.CreateAsync(new CreateClientRequest(
            "Another",
            "Retail",
            "CLIENT@TEST.LOCAL",
            80));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal(1, await database.Context.Clients.CountAsync());
    }

    [Fact]
    public async Task UpdateAsyncChangesClientAndKeepsOrderCount()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database, "Old Name", "IT", "old@test.local", 60);
        await AddOrderAsync(database, client);
        var service = new ClientService(database.Context);

        var result = await service.UpdateAsync(client.Id, new UpdateClientRequest(
            "New Name",
            "Retail",
            "new@test.local",
            90));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal("Retail", result.Value.Industry);
        Assert.Equal("new@test.local", result.Value.ContactEmail);
        Assert.Equal(90, result.Value.CreditRating);
        Assert.Equal(1, result.Value.TotalOrdersCount);

        var updated = await database.Context.Clients.SingleAsync();
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateAsyncReturnsNotFoundForMissingClient()
    {
        using var database = new SqliteTestDb();
        var service = new ClientService(database.Context);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateClientRequest(
            "New Name",
            "Retail",
            "new@test.local",
            90));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteAsyncReturnsFalseWhenClientHasOrders()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database, "Client With Orders", "IT", "client@test.local", 70);
        await AddOrderAsync(database, client);
        var service = new ClientService(database.Context);

        var deleted = await service.DeleteAsync(client.Id);

        Assert.False(deleted);
        Assert.Equal(1, await database.Context.Clients.CountAsync());
    }

    [Fact]
    public async Task DeleteAsyncRemovesClientWithoutOrders()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database, "Client Without Orders", "IT", "client@test.local", 70);
        var service = new ClientService(database.Context);

        var deleted = await service.DeleteAsync(client.Id);

        Assert.True(deleted);
        Assert.Equal(0, await database.Context.Clients.CountAsync());
    }

    [Fact]
    public async Task GetAllAsyncFiltersBySearchAndSortsByCreditRating()
    {
        using var database = new SqliteTestDb();
        await AddClientAsync(database, "Alpha Manufacturing", "Manufacturing", "alpha@test.local", 50);
        await AddClientAsync(database, "Beta Retail", "Retail", "beta@test.local", 90);
        var service = new ClientService(database.Context);

        var filtered = await service.GetAllAsync("retail");
        var sorted = await service.GetAllAsync(sortBy: "-creditRating");

        Assert.Single(filtered);
        Assert.Equal("Beta Retail", filtered[0].Name);
        Assert.Equal("Beta Retail", sorted[0].Name);
        Assert.Equal("Alpha Manufacturing", sorted[1].Name);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsClientWithOrderCount()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database, "Client", "IT", "client@test.local", 70);
        await AddOrderAsync(database, client);
        var service = new ClientService(database.Context);

        var result = await service.GetByIdAsync(client.Id);

        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
        Assert.Equal(1, result.TotalOrdersCount);
    }

    private static async Task<Client> AddClientAsync(
        SqliteTestDb database,
        string name,
        string industry,
        string email,
        int creditRating)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = name,
            Industry = industry,
            ContactEmail = email,
            CreditRating = creditRating,
            CreatedAtUtc = DateTime.UtcNow
        };

        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();
        return client;
    }

    private static async Task AddOrderAsync(SqliteTestDb database, Client client)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Client = client,
            Amount = 1000m,
            PaymentTermsDays = 30,
            ItemsCount = 1,
            Status = DomainOrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        database.Context.Orders.Add(order);
        await database.Context.SaveChangesAsync();
    }
}

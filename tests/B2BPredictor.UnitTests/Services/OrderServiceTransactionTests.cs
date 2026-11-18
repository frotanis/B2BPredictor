using B2BPredictor.Application.Contracts;
using B2BPredictor.Domain.Entities;
using B2BPredictor.Infrastructure.Services;
using B2BPredictor.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.UnitTests.Services;

public sealed class OrderServiceTransactionTests
{
    [Fact]
    public async Task CreateAndPredictPersistsOrderAndPrediction()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database);
        var service = new OrderService(database.Context, new SuccessfulPredictionService());

        var result = await service.CreateAndPredictAsync(new CreateOrderRequest(
            client.Id,
            1500m,
            30,
            2,
            "Test order"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(70, result.Value.SuccessProbability);
        Assert.Equal("Низький ризик", result.Value.RiskLevel);
        Assert.Single(result.Value.RiskFactors);

        var order = await database.Context.Orders
            .Include(candidate => candidate.Predictions)
            .ThenInclude(prediction => prediction.RiskFactors)
            .SingleAsync();

        Assert.Equal(1500m, order.Amount);
        Assert.Equal(70, order.SuccessProbability);
        Assert.Single(order.Predictions);
        Assert.Single(order.Predictions.Single().RiskFactors);
    }

    [Fact]
    public async Task CreateAndPredictRollsBackWhenPredictionFails()
    {
        using var database = new SqliteTestDb();
        var client = await AddClientAsync(database);
        var service = new OrderService(database.Context, new FailingPredictionService());

        var result = await service.CreateAndPredictAsync(new CreateOrderRequest(
            client.Id,
            1500m,
            30,
            2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);

        database.Context.ChangeTracker.Clear();
        Assert.Equal(0, await database.Context.Orders.CountAsync());
        Assert.Equal(0, await database.Context.Predictions.CountAsync());
    }

    private static async Task<Client> AddClientAsync(SqliteTestDb database)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Test Client",
            Industry = "IT",
            ContactEmail = "client@test.local",
            CreditRating = 80,
            CreatedAtUtc = DateTime.UtcNow
        };

        database.Context.Clients.Add(client);
        await database.Context.SaveChangesAsync();
        return client;
    }

    private sealed class SuccessfulPredictionService : IPredictionService
    {
        public Task<PredictionResultDto> PredictAsync(OrderPredictionInput input, CancellationToken ct = default)
        {
            return Task.FromResult(new PredictionResultDto(
                70,
                true,
                "Низький ризик",
                new List<string> { "Критичних факторів ризику не виявлено." },
                "test-model"));
        }

        public Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new MlModelMetricsDto("test-model", 0.9, 0.9, 100, DateTime.UtcNow));
        }
    }

    private sealed class FailingPredictionService : IPredictionService
    {
        public Task<PredictionResultDto> PredictAsync(OrderPredictionInput input, CancellationToken ct = default)
        {
            throw new InvalidOperationException("ML service failed.");
        }

        public Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("ML service failed.");
        }
    }
}

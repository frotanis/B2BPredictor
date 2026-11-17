using B2BPredictor.Application.Contracts;
using B2BPredictor.Infrastructure.ML;
using Xunit;

namespace B2BPredictor.UnitTests;

public class PredictionServiceTests : IDisposable
{
    private readonly string _tempModelPath;
    private readonly PredictionService _sut;

    public PredictionServiceTests()
    {
        _tempModelPath = Path.Combine(Path.GetTempPath(), $"test_model_{Guid.NewGuid()}.zip");
        _sut = new PredictionService(_tempModelPath);
    }

    [Fact]
    public async Task GetModelMetricsAsyncShouldReturnValidMetricsAndCreateFile()
    {
        // Act
        var metrics = await _sut.GetModelMetricsAsync();

        // Assert
        Assert.True(File.Exists(_tempModelPath), "Файл натренованої моделі має існувати.");
        Assert.True(metrics.Accuracy >= 0.75, $"Очікувана точність >= 0.75, отримано {metrics.Accuracy}");
        Assert.True(metrics.F1Score >= 0.75, $"Очікуваний F1-score >= 0.75, отримано {metrics.F1Score}");
        Assert.Equal(1500, metrics.TrainingSamplesCount);
    }

    [Fact]
    public async Task PredictAsyncReliableClientReturnsHighSuccessProbability()
    {
        // Arrange (Рейтинг 85 зі 100 - високий)
        var input = new OrderPredictionInput(
            Amount: 5000m,
            Industry: "IT",
            ClientCreditRating: 85,
            PaymentTermsDays: 14,
            ItemsCount: 5,
            PreviousSuccessfulOrdersCount: 12
        );

        // Act
        var result = await _sut.PredictAsync(input);

        // Assert
        Assert.True(result.IsLikelySuccessful);
        Assert.True(result.SuccessProbability >= 70.0);
        Assert.Equal("Низький ризик", result.RiskLevel);
        Assert.Equal("FastTree-v1.0", result.ModelVersion);
    }

    [Fact]
    public async Task PredictAsyncHighRiskOrderReturnsLowProbabilityAndRiskWarnings()
    {
        // Arrange (Рейтинг 25 зі 100 - низький, велика сума, довга відстрочка)
        var input = new OrderPredictionInput(
            Amount: 45000m,
            Industry: "Construction",
            ClientCreditRating: 25,
            PaymentTermsDays: 75,
            ItemsCount: 50,
            PreviousSuccessfulOrdersCount: 0
        );

        // Act
        var result = await _sut.PredictAsync(input);

        // Assert
        Assert.False(result.IsLikelySuccessful);
        Assert.Equal("Високий ризик", result.RiskLevel);
        Assert.True(result.RiskFactors.Count >= 2);
    }

    [Fact]
    public async Task PredictAsyncNullInputThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.PredictAsync(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task PredictAsyncInvalidAmountThrowsArgumentException(decimal invalidAmount)
    {
        var input = new OrderPredictionInput(invalidAmount, "Retail", 70, 15, 2, 1);
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PredictAsync(input));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task PredictAsyncOutOfRangeCreditRatingThrowsArgumentOutOfRangeException(int invalidRating)
    {
        var input = new OrderPredictionInput(1000m, "Retail", invalidRating, 15, 2, 1);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.PredictAsync(input));
    }

    [Fact]
    public async Task PredictAsyncNegativePaymentTermsThrowsArgumentException()
    {
        var input = new OrderPredictionInput(1000m, "Logistics", 65, -5, 3, 2);
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.PredictAsync(input));
    }

    public void Dispose()
    {
        if (File.Exists(_tempModelPath))
        {
            File.Delete(_tempModelPath);
        }

        GC.SuppressFinalize(this);
    }
}
using B2BPredictor.Application.ML;
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
    public void TrainAndSaveModelShouldAchieveHighAccuracyAndCreateFile()
    {
        // Act
        var (accuracy, f1Score) = _sut.TrainAndSaveModel(_tempModelPath);

        // Assert
        Assert.True(File.Exists(_tempModelPath), "Файл натренованої моделі має існувати.");
        Assert.True(accuracy >= 0.75, $"Очікувана точність >= 0.75, отримано {accuracy}");
        Assert.True(f1Score >= 0.75, $"Очікуваний F1-score >= 0.75, отримано {f1Score}");
    }

    [Fact]
    public void PredictReliableClientReturnsHighSuccessProbability()
    {
        // Arrange
        var request = new OrderPredictionRequest(
            Amount: 5000f,
            Industry: "IT",
            CreditRating: 800,
            PaymentTermsDays: 14,
            ItemsCount: 5,
            PreviousSuccessfulOrders: 12
        );

        // Act
        var result = _sut.Predict(request);

        // Assert
        Assert.True(result.IsLikelySuccessful);
        Assert.True(result.SuccessProbability >= 70f);
        Assert.Equal("Низький ризик", result.RiskLevel);
    }

    [Fact]
    public void PredictHighRiskOrderReturnsLowProbabilityAndRiskWarnings()
    {
        // Arrange (Крайній випадок: велика сума, низький рейтинг, довга відстрочка)
        var request = new OrderPredictionRequest(
            Amount: 45000f,
            Industry: "Construction",
            CreditRating: 350,
            PaymentTermsDays: 75,
            ItemsCount: 50,
            PreviousSuccessfulOrders: 0
        );

        // Act
        var result = _sut.Predict(request);

        // Assert
        Assert.False(result.IsLikelySuccessful);
        Assert.Equal("Високий ризик", result.RiskLevel);
        Assert.True(result.RiskFactors.Count >= 2);
    }

    [Fact]
    public void PredictNullRequestThrowsArgumentNullException()
    {
        // Крайній випадок C2: null вхідні дані
        Assert.Throws<ArgumentNullException>(() => _sut.Predict(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void PredictInvalidAmountThrowsArgumentException(float invalidAmount)
    {
        // Крайній випадок C2: нульова або від'ємна сума
        var request = new OrderPredictionRequest(invalidAmount, "Retail", 600, 15, 2, 1);
        Assert.Throws<ArgumentException>(() => _sut.Predict(request));
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(1200)]
    public void PredictOutOfRangeCreditRatingThrowsArgumentOutOfRangeException(int invalidRating)
    {
        // Крайній випадок C2: рейтинг поза межами [0..1000]
        var request = new OrderPredictionRequest(1000f, "Retail", invalidRating, 15, 2, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Predict(request));
    }

    [Fact]
    public void PredictNegativePaymentTermsThrowsArgumentException()
    {
        // Крайній випадок C2: від'ємний термін відстрочки
        var request = new OrderPredictionRequest(1000f, "Logistics", 650, -5, 3, 2);
        Assert.Throws<ArgumentException>(() => _sut.Predict(request));
    }

    [Fact]
    public void PredictZeroItemsCountThrowsArgumentException()
    {
        var request = new OrderPredictionRequest(1000f, "IT", 700, 10, 0, 1);
        Assert.Throws<ArgumentException>(() => _sut.Predict(request));
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PredictEmptyIndustryThrowsArgumentException(string invalidIndustry)
    {
        var request = new OrderPredictionRequest(1000f, invalidIndustry, 700, 10, 2, 1);
    Assert.Throws<ArgumentException>(() => _sut.Predict(request));
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
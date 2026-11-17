using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public sealed record OrderPredictionInput(
    [property: Range(1, 100_000_000)] decimal Amount,
    [property: Required, StringLength(100)] string Industry,
    [property: Range(1, 100)] int ClientCreditRating,
    [property: Range(0, 365)] int PaymentTermsDays,
    [property: Range(1, 10_000)] int ItemsCount,
    [property: Range(0, 10_000)] int PreviousSuccessfulOrdersCount);

public sealed record PredictionResultDto(
    double SuccessProbability,
    bool IsLikelySuccessful,
    string RiskLevel,
    IReadOnlyList<string> RiskFactors,
    string ModelVersion);

public sealed record MlModelMetricsDto(
    string AlgorithmName,
    double Accuracy,
    double F1Score,
    int TrainingSamplesCount,
    DateTime TrainedAtUtc);

public interface IPredictionService
{
    Task<PredictionResultDto> PredictAsync(OrderPredictionInput input, CancellationToken ct = default);
    Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default);
}
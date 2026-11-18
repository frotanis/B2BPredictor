using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public sealed record OrderPredictionInput(
    [Range(1, 100_000_000)] decimal Amount,
    [Required, StringLength(100)] string Industry,
    [Range(1, 100)] int ClientCreditRating,
    [Range(0, 365)] int PaymentTermsDays,
    [Range(1, 10_000)] int ItemsCount,
    [Range(0, 10_000)] int PreviousSuccessfulOrdersCount);

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
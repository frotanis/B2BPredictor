namespace B2BPredictor.Application.ML;

public record OrderPredictionResponse(
    bool IsLikelySuccessful,
    float SuccessProbability,
    string RiskLevel,
    List<string> RiskFactors
);
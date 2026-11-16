namespace B2BPredictor.Application.ML;

public record OrderPredictionRequest(
    float Amount,
    string Industry,
    int CreditRating,
    int PaymentTermsDays,
    int ItemsCount,
    int PreviousSuccessfulOrders
);
namespace B2BPredictor.Application.ML;

public interface IPredictionService
{
    OrderPredictionResponse Predict(OrderPredictionRequest request);
    (double Accuracy, double F1Score) TrainAndSaveModel(string? modelOutputPath = null);
}
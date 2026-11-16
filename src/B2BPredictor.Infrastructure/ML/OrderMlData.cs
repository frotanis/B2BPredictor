using Microsoft.ML.Data;

namespace B2BPredictor.Infrastructure.ML;

public class OrderInputData
{
    [LoadColumn(0)] public float Amount { get; set; }
    [LoadColumn(1)] public string Industry { get; set; } = string.Empty;
    [LoadColumn(2)] public float CreditRating { get; set; }
    [LoadColumn(3)] public float PaymentTermsDays { get; set; }
    [LoadColumn(4)] public float ItemsCount { get; set; }
    [LoadColumn(5)] public float PreviousSuccessfulOrders { get; set; }
    [LoadColumn(6), ColumnName("Label")] public bool IsSuccessful { get; set; }
}

public class OrderPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedIsSuccessful { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}
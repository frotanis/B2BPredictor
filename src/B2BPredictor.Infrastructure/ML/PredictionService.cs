using B2BPredictor.Application.ML;
using Microsoft.ML;

namespace B2BPredictor.Infrastructure.ML;

public class PredictionService : IPredictionService
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _trainedModel;
    private readonly object _lock = new();

    public PredictionService(string? modelPath = null)
    {
        _mlContext = new MLContext(seed: 42);
        _modelPath = modelPath ?? Path.Combine(AppContext.BaseDirectory, "MLModels", "b2b_order_model.zip");

        if (File.Exists(_modelPath))
        {
            _trainedModel = _mlContext.Model.Load(_modelPath, out _);
        }
        else
        {
            TrainAndSaveModel(_modelPath);
        }
    }

    public (double Accuracy, double F1Score) TrainAndSaveModel(string? modelOutputPath = null)
    {
        var targetPath = modelOutputPath ?? _modelPath;
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rawData = SyntheticDatasetGenerator.Generate(1500);
        var dataView = _mlContext.Data.LoadFromEnumerable(rawData);

        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "IndustryEncoded",
                inputColumnName: nameof(OrderInputData.Industry))
            .Append(_mlContext.Transforms.Concatenate(
                "Features",
                nameof(OrderInputData.Amount),
                "IndustryEncoded",
                nameof(OrderInputData.CreditRating),
                nameof(OrderInputData.PaymentTermsDays),
                nameof(OrderInputData.ItemsCount),
                nameof(OrderInputData.PreviousSuccessfulOrders)))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10));

        lock (_lock)
        {
            _trainedModel = pipeline.Fit(split.TrainSet);
            _mlContext.Model.Save(_trainedModel, dataView.Schema, targetPath);
        }

        var predictions = _trainedModel.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        return (Math.Round(metrics.Accuracy, 4), Math.Round(metrics.F1Score, 4));
    }

    public OrderPredictionResponse Predict(OrderPredictionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0)
            throw new ArgumentException("Сума замовлення повинна бути більшою за 0.", nameof(request));

        if (request.CreditRating is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(request), "Кредитний рейтинг має бути в межах від 0 до 1000.");

        if (request.PaymentTermsDays < 0)
            throw new ArgumentException("Термін відстрочки не може бути від'ємним.", nameof(request));

        if (request.ItemsCount <= 0)
            throw new ArgumentException("Кількість позицій має бути принаймні 1.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Industry))
            throw new ArgumentException("Галузь клієнта не може бути порожньою.", nameof(request));

        if (_trainedModel is null)
        {
            TrainAndSaveModel(_modelPath);
        }

        var input = new OrderInputData
        {
            Amount = request.Amount,
            Industry = request.Industry.Trim(),
            CreditRating = request.CreditRating,
            PaymentTermsDays = request.PaymentTermsDays,
            ItemsCount = request.ItemsCount,
            PreviousSuccessfulOrders = request.PreviousSuccessfulOrders
        };

        OrderPredictionOutput prediction;
        lock (_lock)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<OrderInputData, OrderPredictionOutput>(_trainedModel!);
            prediction = predictionEngine.Predict(input);
        }

        var probabilityPercent = (float)Math.Round(prediction.Probability * 100, 2);
        var risks = AnalyzeRiskFactors(request);

        var riskLevel = probabilityPercent switch
        {
            >= 75f => "Низький ризик",
            >= 45f => "Помірний ризик",
            _ => "Високий ризик"
        };

        return new OrderPredictionResponse(
            IsLikelySuccessful: prediction.PredictedIsSuccessful,
            SuccessProbability: probabilityPercent,
            RiskLevel: riskLevel,
            RiskFactors: risks
        );
    }

    private static List<string> AnalyzeRiskFactors(OrderPredictionRequest req)
    {
        var risks = new List<string>();

        if (req.PaymentTermsDays > 45)
            risks.Add($"Високий ризик затримки оплати через тривалу відстрочку ({req.PaymentTermsDays} днів).");

        if (req.CreditRating < 500)
            risks.Add($"Низький кредитний рейтинг клієнта ({req.CreditRating}). Рекомендована часткова передоплата.");

        if (req.Amount > 30000 && req.PreviousSuccessfulOrders == 0)
            risks.Add("Велика сума замовлення для нового клієнта без історії попередніх угод.");

        if (risks.Count == 0)
            risks.Add("Критичних факторів ризику не виявлено.");

        return risks;
    }
}
using B2BPredictor.Application.Contracts;
using Microsoft.ML;

namespace B2BPredictor.Infrastructure.ML;

public class PredictionService : IPredictionService
{
    private const string CurrentModelVersion = "FastTree-v1.0";
    private const int DefaultTrainingSamples = 1500;

    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _trainedModel;
    private MlModelMetricsDto? _cachedMetrics;
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

    public MlModelMetricsDto TrainAndSaveModel(string? modelOutputPath = null)
    {
        var targetPath = modelOutputPath ?? _modelPath;
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rawData = SyntheticDatasetGenerator.Generate(DefaultTrainingSamples);
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

        _cachedMetrics = new MlModelMetricsDto(
            AlgorithmName: "FastTreeBinaryClassification",
            Accuracy: Math.Round(metrics.Accuracy, 4),
            F1Score: Math.Round(metrics.F1Score, 4),
            TrainingSamplesCount: DefaultTrainingSamples,
            TrainedAtUtc: DateTime.UtcNow
        );

        return _cachedMetrics;
    }

    public Task<PredictionResultDto> PredictAsync(OrderPredictionInput input, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(input);

        if (input.Amount <= 0)
            throw new ArgumentException("Сума замовлення повинна бути більшою за 0.", nameof(input));

        if (input.ClientCreditRating is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(input), "Кредитний рейтинг має бути в межах від 1 до 100.");

        if (input.PaymentTermsDays < 0)
            throw new ArgumentException("Термін відстрочки не може бути від'ємним.", nameof(input));

        if (input.ItemsCount <= 0)
            throw new ArgumentException("Кількість позицій має бути принаймні 1.", nameof(input));

        if (string.IsNullOrWhiteSpace(input.Industry))
            throw new ArgumentException("Галузь клієнта не може бути порожньою.", nameof(input));

        if (_trainedModel is null)
        {
            TrainAndSaveModel(_modelPath);
        }

        var mlInput = new OrderInputData
        {
            Amount = (float)input.Amount,
            Industry = input.Industry.Trim(),
            CreditRating = input.ClientCreditRating,
            PaymentTermsDays = input.PaymentTermsDays,
            ItemsCount = input.ItemsCount,
            PreviousSuccessfulOrders = input.PreviousSuccessfulOrdersCount
        };

        OrderPredictionOutput prediction;
        lock (_lock)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<OrderInputData, OrderPredictionOutput>(_trainedModel!);
            prediction = predictionEngine.Predict(mlInput);
        }

        var probabilityPercent = Math.Round(prediction.Probability * 100.0, 2);
        var risks = AnalyzeRiskFactors(input);

        var riskLevel = probabilityPercent switch
        {
            >= 75.0 => "Низький ризик",
            >= 45.0 => "Помірний ризик",
            _ => "Високий ризик"
        };

        var result = new PredictionResultDto(
            SuccessProbability: probabilityPercent,
            IsLikelySuccessful: prediction.PredictedIsSuccessful,
            RiskLevel: riskLevel,
            RiskFactors: risks,
            ModelVersion: CurrentModelVersion
        );

        return Task.FromResult(result);
    }

    public Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_cachedMetrics is null)
        {
            TrainAndSaveModel(_modelPath);
        }

        return Task.FromResult(_cachedMetrics!);
    }

    private static List<string> AnalyzeRiskFactors(OrderPredictionInput req)
    {
        var risks = new List<string>();

        if (req.PaymentTermsDays > 45)
            risks.Add($"Високий ризик затримки оплати через тривалу відстрочку ({req.PaymentTermsDays} днів).");

        if (req.ClientCreditRating < 40)
            risks.Add($"Низький кредитний рейтинг клієнта ({req.ClientCreditRating}/100). Рекомендована часткова передоплата.");

        if (req.Amount > 30000m && req.PreviousSuccessfulOrdersCount == 0)
            risks.Add("Велика сума замовлення для нового клієнта без історії попередніх угод.");

        if (risks.Count == 0)
            risks.Add("Критичних факторів ризику не виявлено.");

        return risks;
    }
}
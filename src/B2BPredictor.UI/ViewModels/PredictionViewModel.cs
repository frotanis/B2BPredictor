using System.Collections.ObjectModel;
using System.Globalization;
using B2BPredictor.Application.Contracts;
using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace B2BPredictor.UI.ViewModels;

/// <summary>Сценарій «що, якщо»: прогноз успішності угоди без збереження в БД.</summary>
public sealed partial class PredictionViewModel : BaseViewModel
{
    private readonly IB2BApiClient _api;

    [ObservableProperty]
    private string _amount = "150000";

    [ObservableProperty]
    private string? _industry = "IT";

    [ObservableProperty]
    private double _clientCreditRating = 75;

    [ObservableProperty]
    private double _paymentTermsDays = 30;

    [ObservableProperty]
    private string _itemsCount = "20";

    [ObservableProperty]
    private string _previousSuccessfulOrdersCount = "3";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(ResultProgress))]
    private PredictionResultDto? _result;

    [ObservableProperty]
    private MlModelMetricsDto? _metrics;

    [ObservableProperty]
    private bool _hasRiskFactors;

    public PredictionViewModel(IB2BApiClient api)
    {
        _api = api;
        Title = "ML-прогноз";
    }

    public IReadOnlyList<string> Industries { get; } = IndustryCatalog.All;

    public ObservableCollection<string> RiskFactors { get; } = [];

    public bool HasResult => Result is not null;

    public double ResultProgress => Result is null ? 0 : Math.Clamp(Result.SuccessProbability / 100d, 0d, 1d);

    [RelayCommand]
    private async Task LoadMetricsAsync()
    {
        if (Metrics is not null)
        {
            return;
        }

        try
        {
            Metrics = await _api.GetModelMetricsAsync();
        }
        catch (ApiException)
        {
            // Метрики — допоміжна інформація; їх відсутність не блокує екран.
            Metrics = null;
        }
    }

    [RelayCommand]
    private Task PredictAsync() => RunSafeAsync(async () =>
    {
        if (!TryParseDecimal(Amount, out var amount) || amount is < 1 or > 100_000_000)
        {
            ErrorMessage = "Сума має бути числом від 1 до 100 000 000.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Industry))
        {
            ErrorMessage = "Оберіть галузь.";
            return;
        }

        if (!int.TryParse(ItemsCount, out var items) || items is < 1 or > 10_000)
        {
            ErrorMessage = "Кількість позицій — від 1 до 10 000.";
            return;
        }

        if (!int.TryParse(PreviousSuccessfulOrdersCount, out var previous) || previous is < 0 or > 10_000)
        {
            ErrorMessage = "Кількість попередніх успішних замовлень — від 0 до 10 000.";
            return;
        }

        var input = new OrderPredictionInput(
            amount,
            Industry,
            (int)Math.Round(ClientCreditRating),
            (int)Math.Round(PaymentTermsDays),
            items,
            previous);

        Result = await _api.PreviewPredictionAsync(input);

        RiskFactors.Clear();
        foreach (var factor in Result.RiskFactors)
        {
            RiskFactors.Add(factor);
        }

        HasRiskFactors = RiskFactors.Count > 0;
    });

    [RelayCommand]
    private void Reset()
    {
        Result = null;
        RiskFactors.Clear();
        HasRiskFactors = false;
        ErrorMessage = null;
    }

    private static bool TryParseDecimal(string? text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}

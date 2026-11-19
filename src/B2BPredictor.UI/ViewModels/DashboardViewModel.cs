using System.Collections.ObjectModel;
using B2BPredictor.Application.Contracts;
using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace B2BPredictor.UI.ViewModels;

/// <summary>Рядок статистики по галузі з готовим значенням для ProgressBar (0..1).</summary>
public sealed record IndustryStatsItem(
    string Industry,
    int OrdersCount,
    double AverageSuccessProbability,
    decimal TotalAmount)
{
    public double Progress => Math.Clamp(AverageSuccessProbability / 100d, 0d, 1d);
}

public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly IB2BApiClient _api;
    private readonly AuthSessionStore _session;

    [ObservableProperty]
    private int _totalClients;

    [ObservableProperty]
    private int _totalOrders;

    [ObservableProperty]
    private int _highRiskOrdersCount;

    [ObservableProperty]
    private double _averageSuccessProbability;

    [ObservableProperty]
    private decimal _totalPipelineAmount;

    [ObservableProperty]
    private string _greeting = string.Empty;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isIndustryEmpty;

    public DashboardViewModel(IB2BApiClient api, AuthSessionStore session)
    {
        _api = api;
        _session = session;
        Title = "Аналітика";
    }

    public ObservableCollection<IndustryStatsItem> ByIndustry { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!_session.IsAuthenticated)
        {
            await GoToLoginAsync();
            return;
        }

        Greeting = $"{_session.FullName} · {_session.Role}";

        await RunSafeAsync(async () =>
        {
            var summary = await _api.GetDashboardSummaryAsync();

            TotalClients = summary.TotalClients;
            TotalOrders = summary.TotalOrders;
            HighRiskOrdersCount = summary.HighRiskOrdersCount;
            AverageSuccessProbability = summary.AverageSuccessProbability;
            TotalPipelineAmount = summary.TotalPipelineAmount;

            ByIndustry.Clear();
            foreach (var row in summary.ByIndustry.OrderByDescending(r => r.TotalAmount))
            {
                ByIndustry.Add(new IndustryStatsItem(
                    row.Industry,
                    row.OrdersCount,
                    row.AverageSuccessProbability,
                    row.TotalAmount));
            }

            HasData = true;
            IsIndustryEmpty = ByIndustry.Count == 0;
        });

        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _session.Clear();
        HasData = false;
        ByIndustry.Clear();
        await GoToLoginAsync();
    }
}

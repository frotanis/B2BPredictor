using B2BPredictor.UI.ViewModels;

namespace B2BPredictor.UI.Pages;

public partial class PredictionPage : ContentPage
{
    private readonly PredictionViewModel _viewModel;

    public PredictionPage(PredictionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadMetricsCommand.Execute(null);
    }
}

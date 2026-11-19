using B2BPredictor.UI.ViewModels;

namespace B2BPredictor.UI.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;

    public OrdersPage(OrdersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}

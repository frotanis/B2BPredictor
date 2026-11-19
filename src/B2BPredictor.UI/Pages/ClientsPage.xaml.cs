using B2BPredictor.UI.ViewModels;

namespace B2BPredictor.UI.Pages;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _viewModel;

    public ClientsPage(ClientsViewModel viewModel)
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

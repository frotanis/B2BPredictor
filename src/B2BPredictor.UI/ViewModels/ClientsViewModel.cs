using System.Collections.ObjectModel;
using B2BPredictor.Application.Contracts;
using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace B2BPredictor.UI.ViewModels;

public sealed partial class ClientsViewModel : BaseViewModel
{
    private readonly IB2BApiClient _api;
    private readonly AuthSessionStore _session;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string? _newIndustry;

    [ObservableProperty]
    private string _newContactEmail = string.Empty;

    [ObservableProperty]
    private double _newCreditRating = 70;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isRefreshing;

    public ClientsViewModel(IB2BApiClient api, AuthSessionStore session)
    {
        _api = api;
        _session = session;
        Title = "Клієнти";
    }

    public ObservableCollection<ClientDto> Clients { get; } = [];

    public IReadOnlyList<string> Industries { get; } = IndustryCatalog.All;

    public bool CanDelete => _session.IsAdmin;

    [RelayCommand]
    private async Task LoadAsync()
    {
        OnPropertyChanged(nameof(CanDelete));
        await RunSafeAsync(async () =>
        {
            var items = await _api.GetClientsAsync(SearchText);
            Clients.Clear();
            foreach (var client in items)
            {
                Clients.Add(client);
            }

            IsEmpty = Clients.Count == 0;
        });

        IsRefreshing = false;
    }

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        ErrorMessage = null;
    }

    [RelayCommand]
    private Task CreateAsync() => RunSafeAsync(async () =>
    {
        var name = NewName.Trim();
        var email = NewContactEmail.Trim();
        var industry = NewIndustry?.Trim();

        if (name.Length < 2)
        {
            ErrorMessage = "Назва клієнта має містити щонайменше 2 символи.";
            return;
        }

        if (string.IsNullOrWhiteSpace(industry))
        {
            ErrorMessage = "Оберіть галузь.";
            return;
        }

        if (!email.Contains('@', StringComparison.Ordinal) || email.Length < 5)
        {
            ErrorMessage = "Введіть коректний email.";
            return;
        }

        var created = await _api.CreateClientAsync(new CreateClientRequest(
            name,
            industry,
            email,
            (int)Math.Round(NewCreditRating)));

        Clients.Insert(0, created);
        IsEmpty = false;

        NewName = string.Empty;
        NewContactEmail = string.Empty;
        NewIndustry = null;
        NewCreditRating = 70;
        IsFormVisible = false;
    });

    [RelayCommand]
    private async Task DeleteAsync(ClientDto? client)
    {
        if (client is null || !_session.IsAdmin)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Видалення клієнта",
            $"Видалити клієнта «{client.Name}»? Дію неможливо скасувати.",
            "Видалити",
            "Скасувати");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _api.DeleteClientAsync(client.Id);
            Clients.Remove(client);
            IsEmpty = Clients.Count == 0;
        });
    }
}

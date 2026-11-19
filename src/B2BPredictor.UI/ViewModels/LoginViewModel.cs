using B2BPredictor.Application.Contracts;
using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace B2BPredictor.UI.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IB2BApiClient _api;

    [ObservableProperty]
    private string _email = "manager@b2bpredictor.local";

    [ObservableProperty]
    private string _password = "Manager123!";

    [ObservableProperty]
    private string _serverUrl = ApiEndpointSettings.Load();

    public LoginViewModel(IB2BApiClient api)
    {
        _api = api;
        Title = "Вхід";
    }

    [RelayCommand]
    private Task LoginAsync() => RunSafeAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введіть email і пароль.";
            return;
        }

        var baseUri = ApiEndpointSettings.TryNormalize(ServerUrl);
        if (baseUri is null)
        {
            ErrorMessage = "Некоректна адреса сервера. Приклад: http://localhost:8080/";
            return;
        }

        ApiEndpointSettings.Save(baseUri.ToString());
        ServerUrl = baseUri.ToString();

        await _api.LoginAsync(new LoginRequest(Email.Trim(), Password));
        Password = string.Empty;
        await Shell.Current.GoToAsync("//main/dashboard");
    });

    [RelayCommand]
    private void ResetServerUrl() => ServerUrl = ApiEndpointSettings.DefaultBaseUrl;

    [RelayCommand]
    private void FillAdmin()
    {
        Email = "admin@b2bpredictor.local";
        Password = "Admin123!";
    }

    [RelayCommand]
    private void FillManager()
    {
        Email = "manager@b2bpredictor.local";
        Password = "Manager123!";
    }
}

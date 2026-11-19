using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace B2BPredictor.UI.ViewModels;

/// <summary>Базовий клас: стан завантаження, повідомлення про помилку та безпечний запуск дій.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected async Task RunSafeAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await action();
        }
        catch (ApiException ex) when (ex.IsUnauthorized)
        {
            ErrorMessage = ex.Message;
            await GoToLoginAsync();
        }
#pragma warning disable CA1031 // Будь-яка помилка має показуватись користувачу, а не валити застосунок.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static Task GoToLoginAsync() =>
        Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//login");
}

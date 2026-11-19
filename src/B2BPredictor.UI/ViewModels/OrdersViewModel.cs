using System.Collections.ObjectModel;
using System.Globalization;
using B2BPredictor.Application.Contracts;
using B2BPredictor.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace B2BPredictor.UI.ViewModels;

public sealed record StatusFilterOption(string Label, OrderStatus? Status)
{
    public override string ToString() => Label;
}

public sealed partial class OrdersViewModel : BaseViewModel
{
    private const int PageSize = 20;
    private readonly IB2BApiClient _api;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    private int _page = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPages = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    private int _totalCount;

    [ObservableProperty]
    private StatusFilterOption _selectedStatusFilter;

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private ClientDto? _selectedClient;

    [ObservableProperty]
    private string _newAmount = "50000";

    [ObservableProperty]
    private string _newPaymentTermsDays = "30";

    [ObservableProperty]
    private string _newItemsCount = "10";

    [ObservableProperty]
    private string? _newNotes;

    [ObservableProperty]
    private OrderDto? _lastCreatedOrder;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isRefreshing;

    public OrdersViewModel(IB2BApiClient api)
    {
        _api = api;
        Title = "Замовлення";
        _selectedStatusFilter = StatusFilters[0];
    }

    public ObservableCollection<OrderDto> Orders { get; } = [];

    public ObservableCollection<ClientDto> Clients { get; } = [];

    public IReadOnlyList<StatusFilterOption> StatusFilters { get; } =
    [
        new("Усі статуси", null),
        new("Очікує", OrderStatus.Pending),
        new("Схвалено", OrderStatus.Approved),
        new("Відхилено", OrderStatus.Rejected),
        new("Виконано", OrderStatus.Completed)
    ];

    public string PageInfo => $"Сторінка {Page} з {Math.Max(TotalPages, 1)} · всього {TotalCount}";

    partial void OnSelectedStatusFilterChanged(StatusFilterOption value)
    {
        Page = 1;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafeAsync(async () =>
        {
            var result = await _api.GetOrdersAsync(Page, PageSize, SelectedStatusFilter.Status);

            Orders.Clear();
            foreach (var order in result.Items)
            {
                Orders.Add(order);
            }

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(result.TotalPages, 1);
            IsEmpty = Orders.Count == 0;

            if (Clients.Count == 0)
            {
                var clients = await _api.GetClientsAsync();
                foreach (var client in clients)
                {
                    Clients.Add(client);
                }
            }
        });

        IsRefreshing = false;
    }

    private bool CanGoPrevious() => Page > 1;

    private bool CanGoNext() => Page < TotalPages;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPageAsync()
    {
        Page--;
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        Page++;
        await LoadAsync();
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
        if (SelectedClient is null)
        {
            ErrorMessage = "Оберіть клієнта.";
            return;
        }

        if (!decimal.TryParse(NewAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            && !decimal.TryParse(NewAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            ErrorMessage = "Сума має бути числом.";
            return;
        }

        if (amount is < 1 or > 100_000_000)
        {
            ErrorMessage = "Сума має бути від 1 до 100 000 000.";
            return;
        }

        if (!int.TryParse(NewPaymentTermsDays, out var terms) || terms is < 0 or > 365)
        {
            ErrorMessage = "Термін оплати — від 0 до 365 днів.";
            return;
        }

        if (!int.TryParse(NewItemsCount, out var items) || items is < 1 or > 10_000)
        {
            ErrorMessage = "Кількість позицій — від 1 до 10 000.";
            return;
        }

        var notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim();

        var created = await _api.CreateOrderAsync(
            new CreateOrderRequest(SelectedClient.Id, amount, terms, items, notes));

        LastCreatedOrder = created;
        NewNotes = null;
        IsFormVisible = false;

        // Нове замовлення з'являється першим (сортування за датою створення за спаданням).
        Page = 1;
        var refreshed = await _api.GetOrdersAsync(Page, PageSize, SelectedStatusFilter.Status);
        Orders.Clear();
        foreach (var order in refreshed.Items)
        {
            Orders.Add(order);
        }

        TotalCount = refreshed.TotalCount;
        TotalPages = Math.Max(refreshed.TotalPages, 1);
        IsEmpty = Orders.Count == 0;
    });

    [RelayCommand]
    private void DismissLastCreated() => LastCreatedOrder = null;

    [RelayCommand]
    private Task ApproveAsync(OrderDto? order) => ChangeStatusAsync(order, OrderStatus.Approved);

    [RelayCommand]
    private Task RejectAsync(OrderDto? order) => ChangeStatusAsync(order, OrderStatus.Rejected);

    [RelayCommand]
    private Task CompleteAsync(OrderDto? order) => ChangeStatusAsync(order, OrderStatus.Completed);

    private Task ChangeStatusAsync(OrderDto? order, OrderStatus status)
    {
        if (order is null || order.Status == status)
        {
            return Task.CompletedTask;
        }

        return RunSafeAsync(async () =>
        {
            var updated = await _api.UpdateOrderStatusAsync(order.Id, new UpdateOrderStatusRequest(status, order.Notes));
            var index = Orders.IndexOf(order);
            if (index >= 0)
            {
                Orders[index] = updated;
            }
        });
    }
}

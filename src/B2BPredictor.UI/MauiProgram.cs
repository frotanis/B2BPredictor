using B2BPredictor.UI.Pages;
using B2BPredictor.UI.Services;
using B2BPredictor.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace B2BPredictor.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Сервіси. Базова адреса API підставляється в кожен запит з ApiEndpointSettings
        // (за замовчуванням http://localhost:8080/, для Android-емулятора http://10.0.2.2:8080/).
        builder.Services.AddSingleton<AuthSessionStore>();
        builder.Services.AddHttpClient<IB2BApiClient, B2BApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ClientsViewModel>();
        builder.Services.AddTransient<OrdersViewModel>();
        builder.Services.AddTransient<PredictionViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ClientsPage>();
        builder.Services.AddTransient<OrdersPage>();
        builder.Services.AddTransient<PredictionPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

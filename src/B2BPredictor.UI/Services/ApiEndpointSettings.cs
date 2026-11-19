namespace B2BPredictor.UI.Services;

/// <summary>
/// Адреса бекенду. За замовчуванням — Docker-контейнер на порту 8080
/// (Android-емулятор бачить хост-машину як 10.0.2.2). Користувач може змінити адресу
/// на екрані входу, наприклад на http://localhost:5062/ при запуску API через dotnet run.
/// </summary>
public static class ApiEndpointSettings
{
    private const string PreferenceKey = "api_base_url";

    public static string DefaultBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:8080/"
            : "http://localhost:8080/";

    public static string Load() => Preferences.Default.Get(PreferenceKey, DefaultBaseUrl);

    public static void Save(string baseUrl) => Preferences.Default.Set(PreferenceKey, baseUrl);

    /// <summary>Повертає нормалізовану адресу (з кінцевим "/") або null, якщо рядок некоректний.</summary>
    public static Uri? TryNormalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.EndsWith('/'))
        {
            trimmed += "/";
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace B2BPredictor.IntegrationTests;

/// <summary>
/// Тестовий хост із ізольованою тимчасовою SQLite БД для кожного екземпляра фабрики.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestPassword = "P@ssw0rd-Integration-1";

    /// <summary>Спільні JSON-опції (web defaults + enum як рядки).</summary>
    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    private readonly string _dbFileName = $"test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = $"Data Source={_dbFileName};Foreign Keys=True",
            ["Jwt:Key"] = "IntegrationTestSecretKeyMustBeAtLeast32CharsLong!",
            ["Jwt:Issuer"] = "B2BPredictor",
            ["Jwt:Audience"] = "B2BPredictor.Client",
            ["Jwt:ExpiryMinutes"] = "60",
        };

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));

        // Для minimal hosting UseSetting надійніше: значення доступні вже під час builder.Configuration.
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }

    /// <summary>
    /// Реєструє нового тестового менеджера і повертає HttpClient із Bearer-токеном.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken ct = default)
    {
        var client = CreateClient();
        var auth = await RegisterManagerAsync(client, ct: ct);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return client;
    }

    /// <summary>
    /// Реєструє менеджера з унікальним email і повертає відповідь авторизації.
    /// </summary>
    public static async Task<AuthResponse> RegisterManagerAsync(
            HttpClient client,
            string? email = null,
            CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new RegisterRequest(
            FullName: "Test Manager",
            Email: email ?? $"manager_{Guid.NewGuid():N}@test.local",
            Password: TestPassword,
            Role: "Manager");

        using var response = await client.PostAsJsonAsync("/api/auth/register", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, ct);
        return auth ?? throw new InvalidOperationException("Register returned an empty body.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Звільняємо пул з'єднань, інакше файл SQLite залишається заблокованим.
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _dbFileName, $"{_dbFileName}-wal", $"{_dbFileName}-shm", $"{_dbFileName}-journal" })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Тимчасовий тестовий файл: збій видалення не повинен валити тести.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

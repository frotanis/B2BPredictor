using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using B2BPredictor.Application.Contracts;

namespace B2BPredictor.UI.Services;

public sealed class B2BApiClient : IB2BApiClient
{
    // Читання: приймаємо enum і як числа (поточна поведінка API), і як рядки.
    private static readonly JsonSerializerOptions ReadOptions = CreateReadOptions();

    // Запис: enum надсилаються числами — API не реєструє JsonStringEnumConverter.
    private static readonly JsonSerializerOptions WriteOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AuthSessionStore _session;

    public B2BApiClient(HttpClient http, AuthSessionStore session)
    {
        _http = http;
        _session = session;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var auth = await SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, ct);
        _session.SetSession(auth.AccessToken, auth.Role, auth.FullName, auth.ExpiresAtUtc);
        return auth;
    }

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default) =>
        SendAsync<DashboardSummaryDto>(HttpMethod.Get, "api/analytics/summary", null, ct);

    public async Task<IReadOnlyList<ClientDto>> GetClientsAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/clients"
            : $"api/clients?search={Uri.EscapeDataString(search.Trim())}";

        return await SendAsync<List<ClientDto>>(HttpMethod.Get, url, null, ct);
    }

    public Task<ClientDto> CreateClientAsync(CreateClientRequest request, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Post, "api/clients", request, ct);

    public async Task DeleteClientAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(HttpMethod.Delete, $"api/clients/{id}", null, ct);
    }

    public Task<PagedResult<OrderDto>> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        OrderStatus? status = null,
        CancellationToken ct = default)
    {
        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"api/orders?page={page}&pageSize={pageSize}");

        if (status is not null)
        {
            url += $"&status={status}";
        }

        return SendAsync<PagedResult<OrderDto>>(HttpMethod.Get, url, null, ct);
    }

    public Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Post, "api/orders", request, ct);

    public Task<OrderDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default) =>
        SendAsync<OrderDto>(HttpMethod.Patch, $"api/orders/{id}/status", request, ct);

    public Task<PredictionResultDto> PreviewPredictionAsync(OrderPredictionInput input, CancellationToken ct = default) =>
        SendAsync<PredictionResultDto>(HttpMethod.Post, "api/predictions/preview", input, ct);

    public Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default) =>
        SendAsync<MlModelMetricsDto>(HttpMethod.Get, "api/predictions/metrics", null, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string relativeUrl, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, relativeUrl, body, ct);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(ReadOptions, ct)
                ?? throw new ApiException(response.StatusCode, "Порожня відповідь сервера.");
        }
        catch (JsonException ex)
        {
            throw new ApiException(response.StatusCode, "Сервер повернув дані в неочікуваному форматі.", ex);
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken ct)
    {
        var baseUri = ApiEndpointSettings.TryNormalize(ApiEndpointSettings.Load())
            ?? new Uri(ApiEndpointSettings.DefaultBaseUrl);

        using var request = new HttpRequestMessage(method, new Uri(baseUri, relativeUrl));
        if (_session.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: WriteOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(
                null,
                $"Не вдалося з'єднатися з сервером {baseUri}. Перевірте, що API запущено.",
                ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(null, "Сервер не відповів вчасно. Спробуйте ще раз.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _session.Clear();
            }

            var message = await ReadProblemMessageAsync(response, ct);
            throw new ApiException(response.StatusCode, message);
        }
    }

    private static async Task<string> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var fallback = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Сесія недійсна або невірний email/пароль.",
            HttpStatusCode.Forbidden => "Недостатньо прав для цієї дії (потрібна роль Admin).",
            HttpStatusCode.NotFound => "Ресурс не знайдено.",
            _ => $"Помилка API ({(int)response.StatusCode})."
        };

        try
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            // ValidationProblemDetails: { "errors": { "Field": ["msg"] } }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(v => v.GetString())
                        : [p.Value.ToString()])
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                if (messages.Count > 0)
                {
                    return string.Join(Environment.NewLine, messages);
                }
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString() ?? fallback;
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString() ?? fallback;
            }

            return fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static JsonSerializerOptions CreateReadOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.IntegrationTests;

public sealed class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = CustomWebApplicationFactory.JsonOptions;

    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheckReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/healthz", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndToEndRegisterClientOrderAndAnalyticsWorks()
    {
        using var client = _factory.CreateClient();

        // 1. Реєстрація
        var auth = await CustomWebApplicationFactory.RegisterManagerAsync(client, ct: CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("Manager", auth.Role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // 2. Створення клієнта
        var clientDto = await CreateClientAsync(client);

        using var listResponse = await client.GetAsync("/api/clients", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var clients = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<ClientDto>>(Json, CancellationToken.None);
        Assert.NotNull(clients);
        Assert.Contains(clients, c => c.Id == clientDto.Id);

        // 3. Створення замовлення з ML-прогнозом
        var orderRequest = new CreateOrderRequest(
            ClientId: clientDto.Id,
            Amount: 15_000m,
            PaymentTermsDays: 30,
            ItemsCount: 10,
            Notes: "E2E order");

        using var orderResponse = await client.PostAsJsonAsync("/api/orders", orderRequest, Json, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>(Json, CancellationToken.None);
        Assert.NotNull(order);
        Assert.NotNull(order.SuccessProbability);
        Assert.InRange(order.SuccessProbability.Value, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(order.RiskLevel));

        // 4. Замовлення доступне за id
        using var getResponse = await client.GetAsync($"/api/orders/{order.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 5. Дашборд аналітики
        using var summaryResponse = await client.GetAsync("/api/analytics/summary", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>(Json, CancellationToken.None);
        Assert.NotNull(summary);
    }

    [Fact]
    public async Task CreateOrderWithoutTokenReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new CreateOrderRequest(Guid.NewGuid(), 1_000m, 30, 1, null);
        using var response = await client.PostAsJsonAsync("/api/orders", request, Json, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid():N}@test.local";
        await CustomWebApplicationFactory.RegisterManagerAsync(client, email, CancellationToken.None);

        var login = new LoginRequest(email, "definitely-wrong-password");
        using var response = await client.PostAsJsonAsync("/api/auth/login", login, Json, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    public async Task CreateOrderWithNonPositiveAmountReturnsBadRequest(int amount)
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(CancellationToken.None);
        var clientDto = await CreateClientAsync(client);

        var request = new CreateOrderRequest(clientDto.Id, amount, 30, 5, null);
        using var response = await client.PostAsJsonAsync("/api/orders", request, Json, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, CancellationToken.None);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task CreateOrderForUnknownClientReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(CancellationToken.None);

        var request = new CreateOrderRequest(Guid.NewGuid(), 5_000m, 30, 3, null);
        using var response = await client.PostAsJsonAsync("/api/orders", request, Json, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderThatDoesNotExistReturnsNotFound()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(CancellationToken.None);

        using var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PredictionPreviewWithBoundaryValuesReturnsValidPrediction()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync(CancellationToken.None);

        var input = new OrderPredictionInput(
            Amount: 10_000m,
            Industry: "Retail",
            ClientCreditRating: 1,
            PaymentTermsDays: 365,
            ItemsCount: 1,
            PreviousSuccessfulOrdersCount: 0);

        using var response = await client.PostAsJsonAsync("/api/predictions/preview", input, Json, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PredictionResultDto>(Json, CancellationToken.None);
        Assert.NotNull(result);
        Assert.InRange(result.SuccessProbability, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(result.RiskLevel));
    }

    private static async Task<ClientDto> CreateClientAsync(HttpClient client)
    {
        var request = new CreateClientRequest(
            Name: $"Client {Guid.NewGuid():N}",
            Industry: "Retail",
            ContactEmail: $"client_{Guid.NewGuid():N}@test.local",
            CreditRating: 5);

        using var response = await client.PostAsJsonAsync("/api/clients", request, Json, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ClientDto>(Json, CancellationToken.None);
        return dto ?? throw new InvalidOperationException("Create client returned an empty body.");
    }
}
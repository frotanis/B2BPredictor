using B2BPredictor.Application.Contracts;

namespace B2BPredictor.UI.Services;

/// <summary>Типізований клієнт до REST API B2BPredictor.</summary>
public interface IB2BApiClient
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ClientDto>> GetClientsAsync(string? search = null, CancellationToken ct = default);
    Task<ClientDto> CreateClientAsync(CreateClientRequest request, CancellationToken ct = default);
    Task DeleteClientAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<OrderDto>> GetOrdersAsync(
        int page = 1,
        int pageSize = 20,
        OrderStatus? status = null,
        CancellationToken ct = default);

    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);

    Task<PredictionResultDto> PreviewPredictionAsync(OrderPredictionInput input, CancellationToken ct = default);
    Task<MlModelMetricsDto> GetModelMetricsAsync(CancellationToken ct = default);
}

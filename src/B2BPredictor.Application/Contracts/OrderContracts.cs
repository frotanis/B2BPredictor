using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public enum OrderStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3
}

public sealed record OrderDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    decimal Amount,
    int PaymentTermsDays,
    int ItemsCount,
    OrderStatus Status,
    double? SuccessProbability,
    string? RiskLevel,
    IReadOnlyList<string> RiskFactors,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateOrderRequest(
    [property: Required] Guid ClientId,
    [property: Range(1, 100_000_000)] decimal Amount,
    [property: Range(0, 365)] int PaymentTermsDays,
    [property: Range(1, 10_000)] int ItemsCount,
    [property: StringLength(500)] string? Notes = null);

public sealed record UpdateOrderStatusRequest(
    [property: Required] OrderStatus Status,
    [property: StringLength(500)] string? Notes = null);

public sealed record OrderFilterQuery(
    OrderStatus? Status = null,
    Guid? ClientId = null,
    string? SortBy = "-createdAt",
    int Page = 1,
    int PageSize = 20);

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(OrderFilterQuery query, CancellationToken ct = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<OrderDto>> CreateAndPredictAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<ServiceResult<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
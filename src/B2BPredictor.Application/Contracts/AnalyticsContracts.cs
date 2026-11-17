namespace B2BPredictor.Application.Contracts;

public sealed record IndustryStatsDto(
    string Industry,
    int OrdersCount,
    double AverageSuccessProbability,
    decimal TotalAmount);

public sealed record DashboardSummaryDto(
    int TotalClients,
    int TotalOrders,
    int HighRiskOrdersCount,
    double AverageSuccessProbability,
    decimal TotalPipelineAmount,
    IReadOnlyList<IndustryStatsDto> ByIndustry);

public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
}
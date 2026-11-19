using B2BPredictor.Application.Contracts;
using B2BPredictor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.Infrastructure.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private const string HighRiskLevel = "Високий ризик";
    private const double HighRiskProbabilityThreshold = 45d;

    private readonly B2BPredictorDbContext _db;

    public AnalyticsService(B2BPredictorDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        int totalClients = await _db.Clients.AsNoTracking().CountAsync(ct);

        var orderRows = await _db.Orders
            .AsNoTracking()
            .Select(o => new
            {
                Industry = o.Client != null ? o.Client.Industry : "Інше",
                o.Amount,
                o.SuccessProbability,
                o.RiskLevel
            })
            .ToListAsync(ct);

        int totalOrders = orderRows.Count;

        int highRiskOrders = orderRows.Count(o =>
            o.RiskLevel == HighRiskLevel
            || (o.SuccessProbability.HasValue && o.SuccessProbability.Value < HighRiskProbabilityThreshold));

        var probabilities = orderRows
            .Where(o => o.SuccessProbability.HasValue)
            .Select(o => o.SuccessProbability!.Value)
            .ToList();

        double averageProbability = probabilities.Count > 0
            ? Math.Round(probabilities.Average(), 2)
            : 0d;

        decimal totalPipelineAmount = orderRows.Sum(o => o.Amount);

        List<IndustryStatsDto> byIndustry = orderRows
            .GroupBy(o => o.Industry)
            .Select(g =>
            {
                var groupProbs = g
                    .Where(x => x.SuccessProbability.HasValue)
                    .Select(x => x.SuccessProbability!.Value)
                    .ToList();

                double avgGroupProb = groupProbs.Count > 0
                    ? Math.Round(groupProbs.Average(), 2)
                    : 0d;

                return new IndustryStatsDto(
                    g.Key,
                    g.Count(),
                    avgGroupProb,
                    g.Sum(x => x.Amount));
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return new DashboardSummaryDto(
            totalClients,
            totalOrders,
            highRiskOrders,
            averageProbability,
            totalPipelineAmount,
            byIndustry);
    }
}
using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        ArgumentNullException.ThrowIfNull(analytics);
        _analytics = analytics;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct = default)
    {
        DashboardSummaryDto summary = await _analytics.GetDashboardSummaryAsync(ct);
        return Ok(summary);
    }
}
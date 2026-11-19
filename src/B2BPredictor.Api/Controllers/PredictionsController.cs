using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/predictions")]
[Produces("application/json")]
public sealed class PredictionsController : ApiControllerBase
{
    private readonly IPredictionService _predictions;

    public PredictionsController(IPredictionService predictions)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        _predictions = predictions;
    }

    /// <summary>Попередній прогноз без збереження в БД.</summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PredictionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PredictionResultDto>> Preview(
        [FromBody] OrderPredictionInput input,
        CancellationToken ct = default)
    {
        if (input.Amount <= 0)
        {
            return BadRequestProblem("Сума замовлення має бути більшою за нуль.");
        }

        if (input.PaymentTermsDays < 0)
        {
            return BadRequestProblem("Термін оплати не може бути від'ємним.");
        }

        if (input.ItemsCount < 1)
        {
            return BadRequestProblem("Замовлення має містити щонайменше одну позицію.");
        }

        PredictionResultDto result = await _predictions.PredictAsync(input, ct);
        return Ok(result);
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(MlModelMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MlModelMetricsDto>> GetMetrics(CancellationToken ct = default)
    {
        MlModelMetricsDto metrics = await _predictions.GetModelMetricsAsync(ct);
        return Ok(metrics);
    }
}
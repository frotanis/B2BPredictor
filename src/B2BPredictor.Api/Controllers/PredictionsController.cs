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
        if (input.ClientCreditRating is < 1 or > 100)
        {
            return BadRequestProblem("Кредитний рейтинг клієнта має бути в межах від 1 до 100.");
        }

        if (input.Amount <= 0)
        {
            return BadRequestProblem("Сума замовлення має бути більшою за 0.");
        }

        if (input.PaymentTermsDays is < 0 or > 365)
        {
            return BadRequestProblem("Термін оплати має бути в межах від 0 до 365 днів.");
        }

        if (input.ItemsCount < 1)
        {
            return BadRequestProblem("Кількість позицій має бути не менше 1.");
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
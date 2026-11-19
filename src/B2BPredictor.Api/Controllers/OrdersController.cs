using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ApiControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        _orders = orders;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders(
        [FromQuery] OrderFilterQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1)
        {
            return BadRequestProblem("Параметр 'page' має бути не менше 1.");
        }

        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
        {
            return BadRequestProblem($"Параметр 'pageSize' має бути в межах від 1 до {MaxPageSize}.");
        }

        PagedResult<OrderDto> result = await _orders.GetOrdersAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct = default)
    {
        OrderDto? order = await _orders.GetByIdAsync(id, ct);
        return order is null
            ? NotFoundProblem($"Замовлення з id '{id}' не знайдено.")
            : Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct = default)
    {
        ServiceResult<OrderDto> result = await _orders.CreateAndPredictAsync(request, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            return ServiceProblem(result.ErrorMessage, result.ErrorType);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct = default)
    {
        ServiceResult<OrderDto> result = await _orders.UpdateStatusAsync(id, request, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            return ServiceProblem(result.ErrorMessage, result.ErrorType);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        bool deleted = await _orders.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : NotFoundProblem($"Замовлення з id '{id}' не знайдено.");
    }
}
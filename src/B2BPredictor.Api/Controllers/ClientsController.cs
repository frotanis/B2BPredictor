using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
[Produces("application/json")]
public sealed class ClientsController : ApiControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(IClientService clients)
    {
        ArgumentNullException.ThrowIfNull(clients);
        _clients = clients;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sortBy = "name",
        CancellationToken ct = default)
    {
        IReadOnlyList<ClientDto> result = await _clients.GetAllAsync(search, sortBy, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct = default)
    {
        ClientDto? client = await _clients.GetByIdAsync(id, ct);
        return client is null
            ? NotFoundProblem($"Клієнта з id '{id}' не знайдено.")
            : Ok(client);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientDto>> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken ct = default)
    {
        ServiceResult<ClientDto> result = await _clients.CreateAsync(request, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            return ServiceProblem(result.ErrorMessage, result.ErrorType);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientDto>> Update(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken ct = default)
    {
        ServiceResult<ClientDto> result = await _clients.UpdateAsync(id, request, ct);

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
        bool deleted = await _clients.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : NotFoundProblem($"Клієнта з id '{id}' не знайдено.");
    }
}
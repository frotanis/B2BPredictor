using System.Security.Claims;
using B2BPredictor.Api.Models;
using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequestModel request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(
            new RegisterRequest(request.FullName, request.Email, request.Password, request.Role),
            ct);

        return ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequestModel request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(new LoginRequest(request.Email, request.Password), ct);
        return ToActionResult(result);
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var profile = await _authService.GetProfileAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Validation => BadRequest(new ProblemDetails
            {
                Title = "Validation error",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status400BadRequest
            }),
            ServiceErrorType.Conflict => Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status409Conflict
            }),
            ServiceErrorType.Unauthorized => Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status401Unauthorized
            }),
            ServiceErrorType.NotFound => NotFound(),
            _ => BadRequest()
        };
    }
}

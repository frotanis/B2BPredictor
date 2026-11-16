using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public sealed record RegisterRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string FullName,
    [property: Required, EmailAddress, StringLength(150)] string Email,
    [property: Required, StringLength(100, MinimumLength = 6)] string Password,
    [property: Required, StringLength(30)] string Role);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    string Email,
    string Role);

public sealed record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAtUtc);

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);
}
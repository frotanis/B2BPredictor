using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public sealed record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Password,
    [Required, StringLength(30)] string Role);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

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
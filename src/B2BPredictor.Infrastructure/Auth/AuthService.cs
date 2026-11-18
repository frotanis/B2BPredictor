using System.ComponentModel.DataAnnotations;
using B2BPredictor.Application.Contracts;
using B2BPredictor.Domain.Entities;
using B2BPredictor.Domain.Enums;
using B2BPredictor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly B2BPredictorDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(
        B2BPredictorDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
        {
            return ServiceResult.Failure<AuthResponse>("Повне ім'я повинно містити щонайменше 2 символи.", ServiceErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return ServiceResult.Failure<AuthResponse>("Пароль повинен містити щонайменше 6 символів.", ServiceErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email.Trim()))
        {
            return ServiceResult.Failure<AuthResponse>("Некоректний email.", ServiceErrorType.Validation);
        }

        if (!string.Equals(request.Role, UserRole.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Failure<AuthResponse>("Публічна реєстрація доступна лише для ролі Manager.", ServiceErrorType.Validation);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(user => user.Email == email, ct))
        {
            return ServiceResult.Failure<AuthResponse>("Користувач з таким email вже існує.", ServiceErrorType.Conflict);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Manager,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _tokenGenerator.Generate(user);
        return ServiceResult.Success(new AuthResponse(
            token.Token,
            token.ExpiresAtUtc,
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString()));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ServiceResult.Failure<AuthResponse>("Невірний email або пароль.", ServiceErrorType.Unauthorized);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return ServiceResult.Failure<AuthResponse>("Невірний email або пароль.", ServiceErrorType.Unauthorized);
        }

        var token = _tokenGenerator.Generate(user);
        return ServiceResult.Success(new AuthResponse(
            token.Token,
            token.ExpiresAtUtc,
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString()));
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, ct);

        return user is null
            ? null
            : new UserProfileDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.CreatedAtUtc);
    }
}

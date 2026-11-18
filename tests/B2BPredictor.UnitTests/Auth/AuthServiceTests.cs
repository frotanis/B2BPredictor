using System.IdentityModel.Tokens.Jwt;
using B2BPredictor.Application.Contracts;
using B2BPredictor.Infrastructure.Auth;
using B2BPredictor.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace B2BPredictor.UnitTests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterManagerCreatesUserAndToken()
    {
        using var database = new SqliteTestDb();
        var hasher = new Pbkdf2PasswordHasher();
        var service = CreateService(database, hasher);

        var result = await service.RegisterAsync(new RegisterRequest(
            "Test Manager",
            "manager@test.local",
            "Password123!",
            "Manager"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Manager", result.Value.Role);

        var user = await database.Context.Users.SingleAsync();
        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.True(hasher.Verify("Password123!", user.PasswordHash));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        Assert.Contains(token.Claims, claim =>
            claim.Value == "Manager" &&
            claim.Type.Contains("role", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterAdminIsRejected()
    {
        using var database = new SqliteTestDb();
        var service = CreateService(database, new Pbkdf2PasswordHasher());

        var result = await service.RegisterAsync(new RegisterRequest(
            "Test Admin",
            "admin@test.local",
            "Password123!",
            "Admin"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RegisterDuplicateEmailReturnsConflict()
    {
        using var database = new SqliteTestDb();
        var service = CreateService(database, new Pbkdf2PasswordHasher());
        var request = new RegisterRequest("Test Manager", "manager@test.local", "Password123!", "Manager");

        var first = await service.RegisterAsync(request);
        var second = await service.RegisterAsync(request);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturnsUnauthorized()
    {
        using var database = new SqliteTestDb();
        var service = CreateService(database, new Pbkdf2PasswordHasher());
        await service.RegisterAsync(new RegisterRequest("Test Manager", "manager@test.local", "Password123!", "Manager"));

        var result = await service.LoginAsync(new LoginRequest("manager@test.local", "WrongPassword123!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task LoginReturnsTokenAndProfile()
    {
        using var database = new SqliteTestDb();
        var service = CreateService(database, new Pbkdf2PasswordHasher());
        var registration = await service.RegisterAsync(new RegisterRequest(
            "Test Manager",
            "manager@test.local",
            "Password123!",
            "Manager"));

        var login = await service.LoginAsync(new LoginRequest("manager@test.local", "Password123!"));
        var profile = await service.GetProfileAsync(registration.Value!.UserId);

        Assert.True(login.IsSuccess);
        Assert.NotNull(login.Value);
        Assert.False(string.IsNullOrWhiteSpace(login.Value.AccessToken));
        Assert.NotNull(profile);
        Assert.Equal("manager@test.local", profile.Email);
        Assert.Equal("Manager", profile.Role);
    }

    [Fact]
    public void PasswordHasherRejectsInvalidHash()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Password123!");

        Assert.False(hasher.Verify("Password123!", "not-a-valid-hash"));
        Assert.False(hasher.Verify("Password123!", hash + "broken"));
    }

    private static AuthService CreateService(SqliteTestDb database, IPasswordHasher hasher)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "B2BPredictor.Tests",
            Audience = "B2BPredictor.Tests.Client",
            Key = new string('k', 64),
            ExpiryMinutes = 60
        });

        return new AuthService(database.Context, hasher, new JwtTokenGenerator(options));
    }
}

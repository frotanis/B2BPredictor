namespace B2BPredictor.UI.Services;

/// <summary>
/// Зберігає JWT-токен і дані поточного користувача протягом сесії застосунку.
/// </summary>
public sealed class AuthSessionStore
{
    public string? AccessToken { get; private set; }
    public string? Role { get; private set; }
    public string? FullName { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && (ExpiresAtUtc is null || ExpiresAtUtc > DateTime.UtcNow);

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public void SetSession(string token, string role, string fullName, DateTime? expiresAtUtc = null)
    {
        AccessToken = token;
        Role = role;
        FullName = fullName;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Clear()
    {
        AccessToken = null;
        Role = null;
        FullName = null;
        ExpiresAtUtc = null;
    }
}

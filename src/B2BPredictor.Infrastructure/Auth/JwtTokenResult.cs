namespace B2BPredictor.Infrastructure.Auth;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAtUtc);

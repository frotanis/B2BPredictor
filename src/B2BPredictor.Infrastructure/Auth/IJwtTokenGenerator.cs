using B2BPredictor.Domain.Entities;

namespace B2BPredictor.Infrastructure.Auth;

public interface IJwtTokenGenerator
{
    JwtTokenResult Generate(AppUser user);
}

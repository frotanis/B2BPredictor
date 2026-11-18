using B2BPredictor.Application.Contracts;
using B2BPredictor.Infrastructure;
using B2BPredictor.Infrastructure.Auth;
using B2BPredictor.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace B2BPredictor.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureRegistersClientService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(
            "Data Source=:memory:",
            new JwtOptions
            {
                Issuer = "B2BPredictor.Tests",
                Audience = "B2BPredictor.Tests.Client",
                Key = new string('k', 64),
                ExpiryMinutes = 60
            });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        Assert.IsType<ClientService>(clientService);
    }
}

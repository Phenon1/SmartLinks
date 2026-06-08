using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedirectService.SmartLinks.Interfaces;
using RedirectService.SmartLinks.Rules;

namespace RedirectService.Tests.Rules;

public class ServiceCollectionTests
{
    [Fact]
    public void AddSmartLinksRegistersRulesClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmartLinks:RedirectPathPrefix"] = "/s",
                ["RulesService:BaseUrl"] = "http://localhost:8081"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmartLinks(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IRulesClient>());
    }
}

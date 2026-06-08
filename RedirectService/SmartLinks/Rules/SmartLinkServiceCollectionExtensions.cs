using RedirectService.SmartLinks.Configuration;
using RedirectService.SmartLinks.Interfaces;

namespace RedirectService.SmartLinks.Rules;

public static class SmartLinkServiceCollectionExtensions
{
    public static IServiceCollection AddSmartLinks(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmartLinkOptions>(configuration.GetSection(SmartLinkOptions.SectionName));

        services.AddHttpClient<IRulesClient, RulesClient>(client =>
        {
            var baseUrl = configuration["RulesService:BaseUrl"] ?? "http://rulesservice:8080";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        return services;
    }
}

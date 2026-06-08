using Microsoft.Extensions.Options;
using RulesService.SmartLinks.Conditions;
using RulesService.SmartLinks.Configuration;
using RulesService.SmartLinks.Context;
using RulesService.SmartLinks.Interfaces;
using RulesService.SmartLinks.Plugins;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RulesService.SmartLinks;

public static class RulesServiceCollectionExtensions
{
    public static IServiceCollection AddRulesEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RulesOptions>(configuration.GetSection(RulesOptions.SectionName));

        services.AddSingleton<IConditionPluginLoader, DllConditionPluginLoader>();
        services.TryAddSingleton<IRuleHttpContextFactory, DefaultRuleHttpContextFactory>();
        services.AddSingleton(provider => new ConditionRegistry(
            provider.GetRequiredService<IConditionPluginLoader>(),
            provider.GetRequiredService<IHostEnvironment>(),
            provider.GetRequiredService<IOptionsMonitor<RulesOptions>>()));
        services.AddSingleton<IRulesCatalog, RulesCatalog>();
        services.AddSingleton<IRuleHandler, RuleHandler>();

        return services;
    }
}

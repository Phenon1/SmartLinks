using RulesService.SmartLinks.Conditions;
using RulesService.SmartLinks.Context;
using RulesService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RulesService.SmartLinks;

public class RuleHandler : IRuleHandler
{
    private readonly ConditionRegistry _conditions;
    private readonly IRulesCatalog _catalog;
    private readonly IRuleHttpContextFactory _contextFactory;

    public RuleHandler(
        ConditionRegistry conditions,
        IRulesCatalog catalog,
        IRuleHttpContextFactory contextFactory)
    {
        _conditions = conditions;
        _catalog = catalog;
        _contextFactory = contextFactory;
    }

    public RuleHandlerResponse Handle(RuleHandlerRequest request)
    {
        var rules = _catalog.Load();
        var link = rules.Links.FirstOrDefault(item =>
            string.Equals(item.Code, request.Code, StringComparison.OrdinalIgnoreCase));

        if (link is null)
        {
            return rules.DefaultUrl is null
                ? RuleHandlerResponse.NotFound()
                : Fallback(rules, rules.DefaultUrl);
        }

        var context = _contextFactory.Create(request);
        foreach (var rule in link.Rules)
        {
            if (rule.Conditions.All(condition => IsMatch(condition, context)))
            {
                return Rule(rules, rule.TargetUrl, rule.Name);
            }
        }

        var fallback = link.FallbackUrl ?? rules.DefaultUrl;
        return fallback is null
            ? RuleHandlerResponse.NotFound()
            : Fallback(rules, fallback);
    }

    private bool IsMatch(Configuration.ConditionOptions condition, RuleHttpContext context)
    {
        return _conditions.TryGet(condition.Type, out var specification)
            && specification.IsMatch(context, condition.Parameters);
    }

    private static RuleHandlerResponse Rule(Configuration.RulesOptions rules, Uri targetUrl, string ruleName)
    {
        var resolvedUrl = ResolveUrl(rules, targetUrl);
        return resolvedUrl is null
            ? RuleHandlerResponse.NotFound()
            : RuleHandlerResponse.Rule(resolvedUrl, ruleName);
    }

    private static RuleHandlerResponse Fallback(Configuration.RulesOptions rules, Uri targetUrl)
    {
        var resolvedUrl = ResolveUrl(rules, targetUrl);
        return resolvedUrl is null
            ? RuleHandlerResponse.NotFound()
            : RuleHandlerResponse.Fallback(resolvedUrl);
    }

    private static Uri? ResolveUrl(Configuration.RulesOptions rules, Uri url)
    {
        if (url.IsAbsoluteUri)
        {
            return url;
        }

        return rules.BaseUrl is { IsAbsoluteUri: true } baseUrl
            ? new Uri(baseUrl, url)
            : null;
    }
}

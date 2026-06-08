using SmartLinks.Contracts;

namespace RulePlugins.Core;

public class BrowserCondition : IRuleCondition
{
    public string Type => "browser";

    public bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var browsers = ConditionParameters.GetSet(parameters, "is");
        return context.Browser is not null && browsers.Contains(context.Browser);
    }
}

using SmartLinks.Contracts;

namespace RulePlugins.Core;

public class CountryCondition : IRuleCondition
{
    public string Type => "country";

    public bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var countries = ConditionParameters.GetSet(parameters, "is");
        return context.Country is not null && countries.Contains(context.Country);
    }
}

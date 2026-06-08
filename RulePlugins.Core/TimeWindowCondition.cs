using SmartLinks.Contracts;

namespace RulePlugins.Core;

public class TimeWindowCondition : IRuleCondition
{
    public string Type => "time";

    public bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var from = ConditionParameters.Get(parameters, "from");
        var to = ConditionParameters.Get(parameters, "to");

        if (!TimeOnly.TryParse(from, out var start) || !TimeOnly.TryParse(to, out var end))
        {
            return false;
        }

        var localNow = context.RequestedAt;
        if (TimeSpan.TryParse(ConditionParameters.Get(parameters, "utcOffset"), out var offset))
        {
            localNow = context.RequestedAt.ToOffset(offset);
        }

        var current = TimeOnly.FromDateTime(localNow.DateTime);
        return start <= end
            ? current >= start && current <= end
            : current >= start || current <= end;
    }
}

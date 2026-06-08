using SmartLinks.Contracts;

namespace RulePlugins.Core;

public class DeviceCondition : IRuleCondition
{
    public string Type => "device";

    public bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var devices = ConditionParameters.GetSet(parameters, "is");
        return devices.Contains(context.Device);
    }
}

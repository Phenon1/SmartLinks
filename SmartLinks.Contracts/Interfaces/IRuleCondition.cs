namespace SmartLinks.Contracts;

public interface IRuleCondition
{
    string Type { get; }

    bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters);
}

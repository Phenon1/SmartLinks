using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Interfaces;

public interface IConditionPluginLoader
{
    IEnumerable<IRuleCondition> Load(string pluginDirectory);
}

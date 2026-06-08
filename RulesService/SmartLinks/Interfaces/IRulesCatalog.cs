using RulesService.SmartLinks.Configuration;

namespace RulesService.SmartLinks.Interfaces;

public interface IRulesCatalog
{
    RulesOptions Load();
}

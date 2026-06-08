using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Interfaces;

public interface IRuleHandler
{
    RuleHandlerResponse Handle(RuleHandlerRequest request);
}

using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Interfaces;

public interface IRuleHttpContextFactory
{
    RuleHttpContext Create(RuleHandlerRequest request);
}

using RulesService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Context;

public class DefaultRuleHttpContextFactory : IRuleHttpContextFactory
{
    public RuleHttpContext Create(RuleHandlerRequest request)
    {
        return new RuleHttpContext(
            request,
            ClientIpAddressResolver.Resolve(request),
            country: null,
            device: null,
            browser: null);
    }
}

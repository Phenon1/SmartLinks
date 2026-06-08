using SmartLinks.Contracts;

namespace RedirectService.SmartLinks.Interfaces;

public interface IRulesClient
{
    Task<RuleHandlerResponse> HandleAsync(HttpContext httpContext, string code);
}

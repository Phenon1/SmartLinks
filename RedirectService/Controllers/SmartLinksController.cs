using Microsoft.AspNetCore.Mvc;
using RedirectService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RedirectService.Controllers;

[ApiController]
[Route("s")]
public class SmartLinksController : ControllerBase
{
    private readonly IRulesClient _rulesClient;

    public SmartLinksController(IRulesClient rulesClient)
    {
        _rulesClient = rulesClient;
    }

    [HttpGet("{code}")]
    [ApiExplorerSettings(IgnoreApi = true)] 
    public async Task<IActionResult> RedirectByCode(string code)
    {
        var decision = await _rulesClient.HandleAsync(HttpContext, code);

        if (!decision.Found || decision.TargetUrl is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-SmartLink-Rule"] = decision.RuleName ?? "fallback";

        return Redirect(decision.TargetUrl.ToString());
    }

    [HttpGet("{code}/resolve")]
    [ProducesResponseType<RuleHandlerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleHandlerResponse>> ResolveByCode(string code)
    {
        return Ok(await _rulesClient.HandleAsync(HttpContext, code));
    }
}

using RedirectService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RedirectService.SmartLinks.Rules;

public class RulesClient : IRulesClient
{
    private const int _retryCount = 3;

    private readonly HttpClient _httpClient;
    private readonly ILogger<RulesClient> _logger;

    public RulesClient(HttpClient httpClient, ILogger<RulesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RuleHandlerResponse> HandleAsync(HttpContext httpContext, string code)
    {
        var request = new RuleHandlerRequest(
            code,
            DateTimeOffset.UtcNow,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Query.ToDictionary(
                item => item.Key,
                item => item.Value.ToString(),
                StringComparer.OrdinalIgnoreCase),
            httpContext.Request.Headers.ToDictionary(
                item => item.Key,
                item => item.Value.ToString(),
                StringComparer.OrdinalIgnoreCase));

        for (var retry = 1; retry <= _retryCount; retry++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/rules/handle", request, httpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<RuleHandlerResponse>(cancellationToken: httpContext.RequestAborted)
                    ?? RuleHandlerResponse.NotFound();
            }
            catch (Exception ex) when (retry < _retryCount && ex is HttpRequestException)
            {
                _logger.LogWarning(
                    ex,
                    "Не удалось получить решение от сервиса правил для умной ссылки {Code} по адресу {BaseAddress}. Повтор {Retry}/{RetryCount}",
                    code,
                    _httpClient.BaseAddress,
                    retry,
                    _retryCount);

                await Task.Delay(TimeSpan.FromMilliseconds(150), httpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Не удалось получить решение от сервиса правил для умной ссылки {Code} по адресу {BaseAddress}",
                    code,
                    _httpClient.BaseAddress);

                return RuleHandlerResponse.NotFound();
            }
        }

        return RuleHandlerResponse.NotFound();
    }
}

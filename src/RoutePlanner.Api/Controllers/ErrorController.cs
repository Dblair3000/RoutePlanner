using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RoutePlanner.Api.Controllers;


[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
//[ApiVersion("1.0")]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    /// <summary>
    /// Error controller
    /// </summary>
    /// <param name="logger"></param>
    public ErrorController(ILogger<ErrorController> logger) => _logger = logger;

    /// <summary>
    /// Logs any unhandled exceptions and returns a 500 Internal Server status
    /// </summary>
    /// <returns></returns>
    [Route("/error")]
    public IActionResult Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        _logger.LogError(context?.Error, "Original request path: '{RequestPath}'", context?.Path);
        return Problem();
    }
}

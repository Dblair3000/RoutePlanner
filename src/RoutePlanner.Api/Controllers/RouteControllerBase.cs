using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoutePlanner.Api.Controllers;


[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class RouteControllerBase : ControllerBase
{
    /// <summary>
    /// Create a BadRequestObjectResult with a custom message.
    /// </summary>
    /// <param name="message">Message for the BadRequest response</param>
    /// <returns>A BadRequestObjectResult</returns>
    [NonAction]
    protected BadRequestObjectResult BadRequestWithMessage(string message)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = message,
            Status = 400,
        };
        return BadRequest(problemDetails);
    }

    /// <summary>
    /// Create a BadRequestObjectResult with a custom message.
    /// </summary>
    /// <param name="errors">List of errors for the BadRequest response</param>
    /// <returns>A BadRequestObjectResult</returns>
    [NonAction]
    protected BadRequestObjectResult BadRequestWithErrors(IEnumerable<string> errors)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "BadRequest",
            Status = 400,
            Extensions = { { "errors", errors } }
        };
        return BadRequest(problemDetails);
    }

    /// <summary>
    /// Create a NotFoundObjectResult with a custom message.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>A NotFoundObjectResult</returns>
    [NonAction]
    protected NotFoundObjectResult NotFoundWithMessage(string message)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = message,
            Status = 404
        };
        return NotFound(problemDetails);
    }
}
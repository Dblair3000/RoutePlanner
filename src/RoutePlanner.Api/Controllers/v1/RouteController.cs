using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RoutePlanner.Domain.Interfaces;
using RoutePlanner.Domain.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RoutePlanner.Api.Controllers.v1;

[Route("v{version:apiVersion}/routes")]
[ApiVersion("1.0")]
public class RouteController : RouteControllerBase
{
    private readonly IRouteService _routeService;

    public RouteController(IRouteService routeService)
    {
        _routeService = routeService;
    }


    [HttpGet]
    [SwaggerOperation("Get RouteData")]
    [SwaggerResponse(200, type: typeof(CombinedRouteResult), description: "RouteData")]
    [SwaggerResponse(400, type: typeof(ProblemDetails), description: "Bad Request")]
    public async Task<IActionResult> Get()
    {
        // Create a RouteRequest with fixed coordinates (New York to Boston)
        var routeRequest = new RouteRequest(40.7128, -74.0060, 42.3601, -71.0589);

        var result = await _routeService.GetRouteWithPointsOfInterestAsync(routeRequest);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation("Get RouteData for custom coordinates")]
    [SwaggerResponse(200, type: typeof(CombinedRouteResult), description: "RouteData")]
    [SwaggerResponse(400, type: typeof(ValidationProblemDetails), description: "Invalid coordinates")]
    public async Task<IActionResult> Post([FromBody] RouteRequest routeRequest)
    {
        // RouteRequestValidator has already run; invalid requests never reach this point
        var result = await _routeService.GetRouteWithPointsOfInterestAsync(routeRequest);
        return Ok(result);
    }
}

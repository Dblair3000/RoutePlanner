using RoutePlanner.Domain.Models;

namespace RoutePlanner.Domain.Interfaces;

/// <summary>
/// Interface for the Route Service that provides operations related to routes and points of interest.
/// </summary>
public interface IRouteService
{
    /// <summary>
    /// Asynchronously retrieves a combined result containing a route and points of interest along or near the route.
    /// </summary>
    /// <param name="request">The request object containing start and end coordinates for the route.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing a <see cref="CombinedRouteResult"/> object
    /// with the calculated route and the associated points of interest.
    /// </returns>
    Task<CombinedRouteResult> GetRouteWithPointsOfInterestAsync(RouteRequest request);
}
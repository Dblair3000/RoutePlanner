namespace RoutePlanner.Domain.Models;

public class CombinedRouteResult
{
    public required Route Route { get; set; }
    public required List<PointsOfInterest> PointsOfInterest { get; set; }
}
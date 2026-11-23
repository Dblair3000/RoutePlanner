namespace RoutePlanner.Domain.Models;

public class PointsOfInterest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Location Location { get; set; } = new();
}
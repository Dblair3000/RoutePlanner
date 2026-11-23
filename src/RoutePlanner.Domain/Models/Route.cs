namespace RoutePlanner.Domain.Models;

public class Route
{
    public int TravelTimeInSeconds { get; set; }
    public int LengthInMeters { get; set; }
    public required List<RouteLeg> Legs { get; set; }
}
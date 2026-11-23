namespace RoutePlanner.Domain.Models;

public class Location
{
    public string Address { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
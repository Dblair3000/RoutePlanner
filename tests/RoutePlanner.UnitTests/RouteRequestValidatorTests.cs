using RoutePlanner.Api.Validators;
using RoutePlanner.Domain.Models;

namespace RoutePlanner.UnitTests;

public class RouteRequestValidatorTests
{
    private readonly RouteRequestValidator _validator = new();

    [Fact]
    public void Valid_coordinates_pass()
    {
        var result = _validator.Validate(new RouteRequest(40.7128, -74.0060, 42.3601, -71.0589));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(90.1, 0, 10, 10, "StartLat")]
    [InlineData(0, -180.5, 10, 10, "StartLon")]
    [InlineData(0, 0, -91, 10, "EndLat")]
    [InlineData(0, 0, 10, 181, "EndLon")]
    public void Out_of_range_coordinates_fail(double startLat, double startLon, double endLat, double endLon, string property)
    {
        var result = _validator.Validate(new RouteRequest(startLat, startLon, endLat, endLon));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Fact]
    public void Identical_start_and_end_fail()
    {
        var result = _validator.Validate(new RouteRequest(39.7392, -104.9903, 39.7392, -104.9903));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("must be different"));
    }
}

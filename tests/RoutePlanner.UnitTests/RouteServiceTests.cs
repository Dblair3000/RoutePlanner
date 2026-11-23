using Microsoft.Extensions.Options;
using RoutePlanner.Application;
using RoutePlanner.Domain.Models;
using System.Net;

namespace RoutePlanner.UnitTests;

public class RouteServiceTests
{
    private const string TomTomHost = "api.tomtom.com";
    private const string FoursquareHost = "api.foursquare.com";

    private const string TomTomJson = """
        {
          "formatVersion": "0.0.12",
          "routes": [
            {
              "summary": { "lengthInMeters": 346120, "travelTimeInSeconds": 13980 },
              "legs": [
                {
                  "summary": { "lengthInMeters": 346120, "travelTimeInSeconds": 13980 },
                  "points": [
                    { "latitude": 40.71280, "longitude": -74.00600 },
                    { "latitude": 41.30000, "longitude": -72.90000 },
                    { "latitude": 42.36010, "longitude": -71.05890 }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string FoursquareJson = """
        {
          "results": [
            {
              "fsq_id": "abc123",
              "name": "Union Oyster House",
              "categories": [ { "id": 13338, "name": "Seafood Restaurant" } ],
              "location": { "address": "41 Union St", "locality": "Boston", "region": "MA", "country": "US" }
            },
            {
              "fsq_id": "def456",
              "name": "No Category Cafe",
              "location": { "locality": "Boston" }
            }
          ]
        }
        """;

    private static readonly RouteRequest NewYorkToBoston = new(40.7128, -74.0060, 42.3601, -71.0589);

    private static RouteService CreateService(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new RouteOptions
        {
            TomTomApiKey = "tomtom-test-key",
            FoursquareApiKey = "foursquare-test-key"
        }));

    [Fact]
    public async Task Maps_TomTom_route_summary_and_points()
    {
        var handler = new StubHttpMessageHandler()
            .RespondTo(TomTomHost, TomTomJson)
            .RespondTo(FoursquareHost, FoursquareJson);

        var result = await CreateService(handler).GetRouteWithPointsOfInterestAsync(NewYorkToBoston);

        Assert.Equal(346120, result.Route.LengthInMeters);
        Assert.Equal(13980, result.Route.TravelTimeInSeconds);
        var leg = Assert.Single(result.Route.Legs);
        Assert.Equal(3, leg.Points.Count);
        Assert.Equal(42.3601, leg.Points[^1].Latitude, 4);
    }

    [Fact]
    public async Task Maps_Foursquare_places_including_missing_fields()
    {
        var handler = new StubHttpMessageHandler()
            .RespondTo(TomTomHost, TomTomJson)
            .RespondTo(FoursquareHost, FoursquareJson);

        var result = await CreateService(handler).GetRouteWithPointsOfInterestAsync(NewYorkToBoston);

        Assert.Equal(2, result.PointsOfInterest.Count);

        var oysterHouse = result.PointsOfInterest[0];
        Assert.Equal("Union Oyster House", oysterHouse.Name);
        Assert.Equal("Seafood Restaurant", oysterHouse.Category);
        Assert.Equal("41 Union St", oysterHouse.Location.Address);
        Assert.Equal("MA", oysterHouse.Location.Region);

        var cafe = result.PointsOfInterest[1];
        Assert.Equal(string.Empty, cafe.Category);
        Assert.Equal(string.Empty, cafe.Location.Address);
    }

    [Fact]
    public async Task Sends_TomTom_key_in_query_and_Foursquare_key_in_header()
    {
        var handler = new StubHttpMessageHandler()
            .RespondTo(TomTomHost, TomTomJson)
            .RespondTo(FoursquareHost, FoursquareJson);

        await CreateService(handler).GetRouteWithPointsOfInterestAsync(NewYorkToBoston);

        var tomTom = handler.Requests.Single(r => r.RequestUri!.Host == TomTomHost);
        Assert.Contains("key=tomtom-test-key", tomTom.RequestUri!.Query);

        var foursquare = handler.Requests.Single(r => r.RequestUri!.Host == FoursquareHost);
        Assert.DoesNotContain("foursquare-test-key", foursquare.RequestUri!.ToString());
        Assert.Equal("foursquare-test-key", foursquare.Headers.GetValues("Authorization").Single());
        Assert.Contains("ll=42.3601,-71.0589", foursquare.RequestUri!.Query);
    }

    [Fact]
    public async Task Throws_when_a_provider_returns_an_error()
    {
        var handler = new StubHttpMessageHandler()
            .RespondTo(TomTomHost, "{}", HttpStatusCode.Forbidden)
            .RespondTo(FoursquareHost, FoursquareJson);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateService(handler).GetRouteWithPointsOfInterestAsync(NewYorkToBoston));
    }

    [Fact]
    public async Task Throws_when_TomTom_returns_no_routes()
    {
        var handler = new StubHttpMessageHandler()
            .RespondTo(TomTomHost, """{ "routes": [] }""")
            .RespondTo(FoursquareHost, FoursquareJson);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(handler).GetRouteWithPointsOfInterestAsync(NewYorkToBoston));
    }
}

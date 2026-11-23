using Microsoft.Extensions.Options;
using RoutePlanner.Domain.Interfaces;
using RoutePlanner.Domain.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoutePlanner.Application;

/// <summary>
/// Combines a driving route from TomTom with nearby points of interest from Foursquare.
/// Both provider calls run in parallel; provider-specific response shapes are mapped
/// to the domain models here so nothing outside this class depends on them.
/// </summary>
public class RouteService : IRouteService
{
    // Foursquare category 13000 = "Dining and Drinking"
    private const int DiningCategoryId = 13000;
    private const int PoiSearchRadiusMeters = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly RouteOptions _routeOptions;

    public RouteService(HttpClient httpClient, IOptions<RouteOptions> routeOptions)
    {
        _httpClient = httpClient;
        _routeOptions = routeOptions.Value;
    }

    /// <inheritdoc />
    public async Task<CombinedRouteResult> GetRouteWithPointsOfInterestAsync(RouteRequest request)
    {
        var routeTask = GetRouteFromTomTomAsync(request);
        var poiTask = GetPointsOfInterestFromFoursquareAsync(request.EndLat, request.EndLon);

        await Task.WhenAll(routeTask, poiTask);

        return new CombinedRouteResult
        {
            Route = await routeTask,
            PointsOfInterest = await poiTask
        };
    }

    private async Task<Route> GetRouteFromTomTomAsync(RouteRequest request)
    {
        var url = FormattableString.Invariant(
            $"https://api.tomtom.com/routing/1/calculateRoute/{request.StartLat},{request.StartLon}:{request.EndLat},{request.EndLon}/json?key={Uri.EscapeDataString(_routeOptions.TomTomApiKey)}");

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TomTomResponse>(JsonOptions);
        var route = body?.Routes?.FirstOrDefault()
            ?? throw new InvalidOperationException("TomTom returned no route for the requested coordinates.");

        return new Route
        {
            LengthInMeters = route.Summary?.LengthInMeters ?? 0,
            TravelTimeInSeconds = route.Summary?.TravelTimeInSeconds ?? 0,
            Legs = (route.Legs ?? Enumerable.Empty<TomTomLeg>()).Select(leg => new RouteLeg
            {
                Points = (leg.Points ?? Enumerable.Empty<TomTomPoint>()).Select(p => new RoutePoint
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude
                }).ToList()
            }).ToList()
        };
    }

    private async Task<List<PointsOfInterest>> GetPointsOfInterestFromFoursquareAsync(double lat, double lon)
    {
        var url = FormattableString.Invariant(
            $"https://api.foursquare.com/v3/places/search?ll={lat},{lon}&radius={PoiSearchRadiusMeters}&categories={DiningCategoryId}");

        // Foursquare expects the API key in the Authorization header, not the query string
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.TryAddWithoutValidation("Authorization", _routeOptions.FoursquareApiKey);

        using var response = await _httpClient.SendAsync(message);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<FoursquareResponse>(JsonOptions);

        return (body?.Results ?? Enumerable.Empty<FoursquarePlace>()).Select(place => new PointsOfInterest
        {
            Name = place.Name ?? string.Empty,
            Category = place.Categories?.FirstOrDefault()?.Name ?? string.Empty,
            Location = new Location
            {
                Address = place.Location?.Address ?? string.Empty,
                Locality = place.Location?.Locality ?? string.Empty,
                Region = place.Location?.Region ?? string.Empty,
                Country = place.Location?.Country ?? string.Empty
            }
        }).ToList();
    }

    // ---- Provider response shapes (only the fields this API uses) ----

    internal sealed record TomTomResponse(List<TomTomRoute>? Routes);
    internal sealed record TomTomRoute(TomTomSummary? Summary, List<TomTomLeg>? Legs);
    internal sealed record TomTomSummary(int LengthInMeters, int TravelTimeInSeconds);
    internal sealed record TomTomLeg(List<TomTomPoint>? Points);
    internal sealed record TomTomPoint(double Latitude, double Longitude);

    internal sealed record FoursquareResponse(List<FoursquarePlace>? Results);
    internal sealed record FoursquarePlace(string? Name, List<FoursquareCategory>? Categories, FoursquareLocation? Location);
    internal sealed record FoursquareCategory(string? Name);
    internal sealed record FoursquareLocation(
        string? Address,
        string? Locality,
        string? Region,
        string? Country);
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoutePlanner.Domain.Interfaces;
using RoutePlanner.Domain.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RoutePlanner.IntegrationTests;

/// <summary>
/// Runs the real API pipeline in memory (auth, validation, versioning, routing)
/// with the external route service swapped for a fake.
/// </summary>
public class AuthAndValidationTests : IClassFixture<AuthAndValidationTests.ApiFactory>
{
    private readonly HttpClient _client;

    public AuthAndValidationTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Token_endpoint_issues_token_for_demo_user()
    {
        var token = await GetTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Token_endpoint_rejects_bad_credentials()
    {
        var response = await _client.PostAsJsonAsync("/api/token/generate",
            new { username = "test", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Routes_require_a_token()
    {
        var response = await _client.GetAsync("/v1.0/routes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_request_returns_route()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1.0/routes")
        {
            Content = JsonContent.Create(new { startLat = 40.7128, startLon = -74.0060, endLat = 42.3601, endLon = -71.0589 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());

        var response = await _client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.OK, Describe(response));
        var body = await response.Content.ReadFromJsonAsync<CombinedRouteResult>();
        Assert.Equal(1000, body!.Route.LengthInMeters);
    }

    [Fact]
    public async Task Invalid_coordinates_return_400_before_calling_providers()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1.0/routes")
        {
            Content = JsonContent.Create(new { startLat = 95.0, startLon = -74.0060, endLat = 42.3601, endLon = -71.0589 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());

        var response = await _client.SendAsync(request);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, Describe(response));
        Assert.Contains("StartLat", await response.Content.ReadAsStringAsync());
    }

    private async Task<string> GetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/token/generate",
            new { username = "test", password = "password" });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.Token;
    }

    // On failure, include the bearer handler's reason (WWW-Authenticate error_description)
    private static string Describe(HttpResponseMessage response) =>
        $"Status {(int)response.StatusCode} {response.StatusCode}. " +
        $"WWW-Authenticate: {string.Join(" | ", response.Headers.WwwAuthenticate)}";

    private sealed record TokenResponse(string Token);

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "RoutePlanner.Tests",
                    ["Jwt:Audience"] = "RoutePlanner.Tests",
                    ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-bytes"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IRouteService, FakeRouteService>();
            });
        }
    }

    private sealed class FakeRouteService : IRouteService
    {
        public Task<CombinedRouteResult> GetRouteWithPointsOfInterestAsync(RouteRequest request) =>
            Task.FromResult(new CombinedRouteResult
            {
                Route = new Route { LengthInMeters = 1000, TravelTimeInSeconds = 60, Legs = [] },
                PointsOfInterest = []
            });
    }
}

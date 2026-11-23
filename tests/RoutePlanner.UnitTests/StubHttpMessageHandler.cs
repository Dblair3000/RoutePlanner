using System.Net;
using System.Text;

namespace RoutePlanner.UnitTests;

/// <summary>
/// Fake HTTP handler: returns canned responses by host and records every request,
/// so RouteService can be tested without calling TomTom or Foursquare.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpMessageHandler RespondTo(string host, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[host] = (status, json);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (!_responses.TryGetValue(request.RequestUri!.Host, out var canned))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(canned.Status)
        {
            Content = new StringContent(canned.Body, Encoding.UTF8, "application/json")
        });
    }
}

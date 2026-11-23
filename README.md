# RoutePlanner API

A .NET 10 Web API that returns a driving route between two points along with places to eat near the destination. It combines two external providers:

- **TomTom Routing API**: route distance, travel time and the route geometry
- **Foursquare Places API**: dining and drinking spots within 500 m of the destination

Both providers are called **in parallel**. Their responses are mapped to provider-neutral domain models, so the rest of the system never depends on a vendor's response format.

## Architecture

```
src/
  RoutePlanner.Api           ASP.NET Core host: controllers, auth, validation, versioning, Swagger
  RoutePlanner.Application   RouteService: provider calls and mapping to domain models
  RoutePlanner.Domain        Domain models and the IRouteService contract (no dependencies)
tests/
  RoutePlanner.UnitTests         RouteService (stubbed HTTP) and validator tests
  RoutePlanner.IntegrationTests  Full pipeline in memory via WebApplicationFactory
```

Dependencies point inward: **Api → Application → Domain**. The domain project has no framework or vendor references.

### Design decisions

| Concern | Approach | Why |
|---|---|---|
| Layering | Clean-architecture style project split | Provider details stay out of the API and domain. Swapping TomTom for another provider only touches `RouteService`. |
| HTTP clients | Typed `HttpClient` via `IHttpClientFactory` | Avoids socket exhaustion and DNS staleness from hand-managed `HttpClient` instances. |
| Provider calls | `Task.WhenAll` over both providers | Response time is the slower of the two calls, not the sum of both. |
| Provider contracts | Internal response records mapped to domain models | Vendor JSON shapes can change without breaking API consumers. |
| Validation | FluentValidation with automatic model validation | Bad coordinates are rejected with a `400 ValidationProblemDetails` **before** any paid provider call is made. |
| Errors | RFC 7807 `ProblemDetails` everywhere, plus a central `/error` handler that logs unhandled exceptions | Clients get one consistent error format. |
| Versioning | `Asp.Versioning` with URL segment (`/v1.0/...`) | Endpoints can evolve without breaking existing clients. |
| Auth | JWT bearer. The signing key, issuer and audience come from configuration and are validated at startup. | No secrets in source. The app fails fast if misconfigured. |
| Secrets | .NET user secrets locally, environment variables or Key Vault when hosted | `appsettings.json` contains no keys. |
| Docs | Swagger/OpenAPI with a bearer-token security scheme | Try the API straight from the browser. |

## Getting started

**Prerequisites:** .NET 10 SDK, a [TomTom developer key](https://developer.tomtom.com/) and a [Foursquare Places API key](https://foursquare.com/developers/).

```bash
cd src/RoutePlanner.Api

dotnet user-secrets set "RouteOptions:TomTomApiKey"     "<your TomTom key>"
dotnet user-secrets set "RouteOptions:FoursquareApiKey" "<your Foursquare key>"
dotnet user-secrets set "Jwt:SigningKey"                "<any random string of 32+ characters>"

dotnet run
```

Swagger UI is at `http://localhost:5183/swagger`.

## Using the API

1. **Get a token** (demo credentials: `test` / `password`):

   ```http
   POST /api/token/generate
   Content-Type: application/json

   { "username": "test", "password": "password" }
   ```

2. **Request a route:**

   ```http
   POST /v1.0/routes
   Authorization: Bearer <token>
   Content-Type: application/json

   { "startLat": 40.7128, "startLon": -74.0060, "endLat": 42.3601, "endLon": -71.0589 }
   ```

   `GET /v1.0/routes` returns a sample New York → Boston route.

**Response (abridged):**

```json
{
  "route": {
    "travelTimeInSeconds": 13980,
    "lengthInMeters": 346120,
    "legs": [ { "points": [ { "latitude": 40.7128, "longitude": -74.006 } ] } ]
  },
  "pointsOfInterest": [
    {
      "name": "Union Oyster House",
      "category": "Seafood Restaurant",
      "location": { "address": "41 Union St", "locality": "Boston", "region": "MA", "country": "US" }
    }
  ]
}
```

**Validation error example** (latitude out of range):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "StartLat": [ "'Start Lat' must be between -90 and 90. You entered 95." ] }
}
```

`src/RoutePlanner.Api/RoutePlanner.Api.http` contains ready-made requests for Visual Studio and VS Code.

## Tests

```bash
dotnet test
```

- **Unit tests** cover `RouteService` against a stubbed `HttpMessageHandler`, so no network calls are made. They check response mapping, missing fields, how each provider's API key is sent, provider errors and empty results. The validator rules are tested too.
- **Integration tests** run the real ASP.NET Core pipeline in memory with `WebApplicationFactory`. They check token issuance, rejected credentials, the 401 for a missing token, the 400 for invalid input, and an authenticated round trip with the route service replaced by a fake.

## What I'd add for production

- Swap the demo token endpoint for a real identity provider (Entra ID, Auth0) and validate its tokens
- Resilience on provider calls (retry with backoff, timeouts, circuit breaker) via `Microsoft.Extensions.Http.Resilience`
- Response caching for repeated coordinate pairs to cut provider cost
- Rate limiting per client, and health checks for both providers
- Structured logging and tracing with OpenTelemetry
- A CI pipeline (build, test, coverage) in GitHub Actions

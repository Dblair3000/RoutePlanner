using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoutePlanner.Domain.Interfaces;


namespace RoutePlanner.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RouteOptions>(configuration.GetSection("RouteOptions"));

        // Typed client: IHttpClientFactory manages the HttpClient lifetime for RouteService
        services.AddHttpClient<IRouteService, RouteService>();

        return services;
    }
}

using FluentValidation;
using RoutePlanner.Domain.Models;

namespace RoutePlanner.Api.Validators;

/// <summary>
/// Validates incoming route requests before they reach the route service,
/// so bad coordinates never cost a call to an external provider.
/// </summary>
public class RouteRequestValidator : AbstractValidator<RouteRequest>
{
    public RouteRequestValidator()
    {
        RuleFor(x => x.StartLat).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.EndLat).InclusiveBetween(-90.0, 90.0);
        RuleFor(x => x.StartLon).InclusiveBetween(-180.0, 180.0);
        RuleFor(x => x.EndLon).InclusiveBetween(-180.0, 180.0);

        RuleFor(x => x)
            .Must(x => x.StartLat != x.EndLat || x.StartLon != x.EndLon)
            .WithName("Route")
            .WithMessage("Start and end coordinates must be different.");
    }
}

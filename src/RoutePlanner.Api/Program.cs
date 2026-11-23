using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoutePlanner.Api.Auth;
using RoutePlanner.Api.Validators;
using RoutePlanner.Application;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddControllers();

// Request validation (FluentValidation) - invalid requests return 400 ProblemDetails
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RouteRequestValidator>();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Include version information in response headers
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; //  ex:"v1"
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();

//register our domain service 
builder.Services.AddApplicationCore(builder.Configuration);


// JWT settings come from configuration (user secrets locally, env vars / Key Vault when hosted).
// Fail fast at startup if the signing key is missing or too short for HS256.
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
        "Jwt:Issuer and Jwt:Audience are required.")
    .Validate(s => Encoding.UTF8.GetByteCount(s.SigningKey ?? string.Empty) >= 32,
        "Jwt:SigningKey must be at least 32 bytes. Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<value>\"")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configure the bearer handler lazily from the bound settings, so nothing secret is hard-coded
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>, ILoggerFactory>((options, jwtOptions, loggerFactory) =>
    {
        var jwt = jwtOptions.Value;
        var logger = loggerFactory.CreateLogger("JwtBearer");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1.0", new OpenApiInfo
    {
        Version = "v1.0",
        Title = "Route API v1.0",
        Description = "API for managing routes and points of interest (v1.0)"
    });

    // Define the JWT Bearer scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add the security requirement for Bearer token authentication
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Automatically select the latest API version in Swagger
    options.DocInclusionPredicate((version, apiDescription) =>
    {
        var versions = apiDescription.ActionDescriptor.EndpointMetadata
            .OfType<ApiVersionAttribute>()
            .SelectMany(attr => attr.Versions);
        return versions.Any(v => $"v{v}" == version);
    });
});

var app = builder.Build();

// Unhandled exceptions are logged by ErrorController and returned as ProblemDetails
app.UseExceptionHandler("/error");


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Automatically display the API version in Swagger without requiring manual input!
        options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Route API v1.0");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes Program to WebApplicationFactory in the integration tests
public partial class Program { }

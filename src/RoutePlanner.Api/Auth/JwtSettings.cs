namespace RoutePlanner.Api.Auth;

/// <summary>
/// JWT issuer settings, bound from the "Jwt" configuration section.
/// The signing key is never committed: set it with user secrets locally
/// or an environment variable / Key Vault reference in hosted environments.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 30;
}

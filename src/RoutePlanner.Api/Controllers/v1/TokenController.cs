using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoutePlanner.Api.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RoutePlanner.Api.Controllers;

/// <summary>
/// Issues demo JWTs. In a real system this would be an identity provider
/// (Entra ID, Auth0, etc.); it exists here so the API can be exercised end to end.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
public class TokenController : ControllerBase
{
    private readonly JwtSettings _jwt;
    private readonly ILogger<TokenController> _logger;

    public TokenController(IOptions<JwtSettings> jwt, ILogger<TokenController> logger)
    {
        _jwt = jwt.Value;
        _logger = logger;
    }

    [HttpPost]
    [Route("generate")]
    [AllowAnonymous]
    public IActionResult GenerateToken([FromBody] LoginModel login)
    {
        // Demo-only credential check.
        if (login.Username != "test" || login.Password != "password")
        {
            _logger.LogWarning("Failed token request for user {Username}", login.Username);
            return Unauthorized();
        }

        return Ok(new { token = GenerateJwtToken(login.Username) });
    }

    private string GenerateJwtToken(string username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.TokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginModel
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

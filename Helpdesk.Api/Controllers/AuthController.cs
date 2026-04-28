using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private static readonly IReadOnlyDictionary<string, TrainingUser> Users =
        new Dictionary<string, TrainingUser>(StringComparer.OrdinalIgnoreCase)
        {
            ["agent@local"] = new("agent@local", "Passw0rd!", "Agent"),
            ["customer@local"] = new("customer@local", "Passw0rd!", "Customer"),
        };

    public AuthController(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        await Task.Yield();

        if (!Users.TryGetValue(req.Email, out var user) || user.Password != req.Password)
            return Unauthorized();

        var issuer = _cfg["Jwt:Issuer"] ?? "helpdesk-lite";
        var audience = _cfg["Jwt:Audience"] ?? "helpdesk-lite";
        var key = _cfg["Jwt:Key"] ?? "";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new LoginResponse(jwt));
    }

    public sealed record LoginRequest([Required] string Email, [Required] string Password);
    public sealed record LoginResponse(string AccessToken);
    private sealed record TrainingUser(string Email, string Password, string Role);
}

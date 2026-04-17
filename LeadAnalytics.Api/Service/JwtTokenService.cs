using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeadAnalytics.Api.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;

namespace LeadAnalytics.Api.Service;

public class JwtTokenService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(
        LoginRequestDto request,
        string role,
        IEnumerable<UnitSelectorOptionDto> units)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "LeadAnalytics.Api";
        var audience = jwtSection["Audience"] ?? "LeadAnalytics.Frontend";
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        var expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var parsed)
            ? parsed
            : 480;

        if (key.Length < 32)
            throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 caracteres.");

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Email ?? request.Name ?? "usuario"),
            new(JwtRegisteredClaimNames.Email, request.Email ?? string.Empty),
            new(ClaimTypes.Name, request.Name ?? "Usuário Cloudia"),
            new(ClaimTypes.Role, role)
        };

        claims.AddRange(units.Select(unit => new Claim("clinic_id", unit.ClinicId.ToString())));
        claims.AddRange(units.Select(unit => new Claim("unit_id", unit.Id.ToString())));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}

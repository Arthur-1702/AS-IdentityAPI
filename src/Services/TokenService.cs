using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Models;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services;

public class TokenService(IConfiguration config)
{
    private readonly string _secret = config["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured.");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "IdentityService";
    private readonly string _audience = config["Jwt:Audience"] ?? "AgroMonitor";
    private readonly int _expiresInMinutes = int.Parse(config["Jwt:ExpiresInMinutes"] ?? "60");

    public (string token, int expiresIn) Generate(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresInMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), _expiresInMinutes * 60);
    }
}

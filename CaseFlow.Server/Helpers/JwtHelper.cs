using CaseFlow.Server.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CaseFlow.Server.Helpers;

public static class JwtHelper
{
    public static string GenerateJwt(User user, IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"] ?? "super_secret_demo_key_2026_long_enough_for_hs256_!!!!!!!!!!!!!!!!";
        var jwtIssuer = config["Jwt:Issuer"] ?? "CaseFlow";
        var jwtAudience = config["Jwt:Audience"] ?? "CaseFlowClients";

        var claims = new List<Claim>
        {
            new Claim("user_id", user.UserId.ToString()),
            new Claim(ClaimTypes.Role, user.Role ?? "SE"),
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

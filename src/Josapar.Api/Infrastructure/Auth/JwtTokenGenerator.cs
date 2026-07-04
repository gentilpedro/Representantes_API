using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Josapar.Api.Infrastructure.Persistence.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Josapar.Api.Infrastructure.Auth;

public class JwtTokenGenerator(IConfiguration configuration)
{
    public string Generate(Representative representative)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey não configurada.");
        var expiryMinutes = jwtSection.GetValue("ExpiryMinutes", 480);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, representative.Id.ToString()),
            new Claim("matricula", representative.MatriculaCode),
            new Claim("name", representative.Name),
            new Claim("role", representative.Role),
            new Claim("region", representative.Region),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

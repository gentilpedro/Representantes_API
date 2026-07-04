using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Josapar.Api.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRepresentativeId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Token sem claim 'sub'.");

        return Guid.Parse(sub);
    }
}

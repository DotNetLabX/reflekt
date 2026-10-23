using System.Security.Claims;
using Blocks.Exceptions;

namespace Reflekt.API.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("User id claim is missing.");
        return int.Parse(value);
    }
}

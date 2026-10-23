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

    public static int? GetParticipantId(this ClaimsPrincipal principal)
        => ParseIntClaim(principal, GuestClaims.ParticipantId);

    public static int? GetSprintId(this ClaimsPrincipal principal)
        => ParseIntClaim(principal, GuestClaims.SprintId);

    private static int? ParseIntClaim(ClaimsPrincipal principal, string claimType)
        => int.TryParse(principal.FindFirstValue(claimType), out var value) ? value : null;
}

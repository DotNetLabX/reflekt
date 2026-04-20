using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Auth;

public class GuestTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ParticipantRepository participantRepository)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-Guest-Token"].FirstOrDefault()
            ?? Request.Query["access_token"].FirstOrDefault();

        if (token is null)
            return AuthenticateResult.NoResult();

        var participant = await participantRepository.GetByGuestTokenAsync(token, Context.RequestAborted);
        if (participant is null)
            return AuthenticateResult.Fail("Invalid guest token");

        var claims = new[]
        {
            new Claim("participantId", participant.Id.ToString()),
            new Claim("sprintId", participant.SprintId.ToString()),
            new Claim("displayName", participant.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, GuestTokenDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

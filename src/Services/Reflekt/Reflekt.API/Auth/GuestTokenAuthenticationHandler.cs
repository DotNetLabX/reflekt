using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Reflekt.API.Auth;

public class GuestTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["X-Guest-Token"].FirstOrDefault()
            ?? Request.Query["access_token"].FirstOrDefault();

        if (token is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        // TODO(feature=Participants): Query Participant by GuestToken, issue claims
        return Task.FromResult(AuthenticateResult.Fail("Participant lookup not yet wired"));
    }
}

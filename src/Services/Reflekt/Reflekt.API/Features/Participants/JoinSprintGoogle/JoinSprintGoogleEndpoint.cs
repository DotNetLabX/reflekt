using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;
using AuthPolicies = Reflekt.API.Auth.Policies;

namespace Reflekt.API.Features.Participants.JoinSprintGoogle;

public sealed record JoinSprintGoogleRequest(int SprintId);

[Authorize(Policy = AuthPolicies.GoogleUser)]
[HttpPost("/api/sprints/{sprintId}/join/google")]
[Tags("Participants")]
public sealed class JoinSprintGoogleEndpoint(
    SprintRepository sprintRepository,
    ParticipantRepository participantRepository,
    UserRepository userRepository)
    : Endpoint<JoinSprintGoogleRequest, JoinSprintResponse>
{
    public override async Task HandleAsync(JoinSprintGoogleRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null) { await SendNotFoundAsync(ct); return; }

        if (sprint.State != SprintState.Active)
            ThrowError("Sprint is not accepting participants.", StatusCodes.Status409Conflict);

        var existing = await participantRepository.GetByUserAndSprintAsync(userId, request.SprintId, ct);
        if (existing is not null)
        {
            if (existing.IsRemoved)
                ThrowError("You were removed from this sprint.", StatusCodes.Status409Conflict);

            await SendAsync(existing.Adapt<JoinSprintResponse>(), 200, ct);
            return;
        }

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) { await SendNotFoundAsync(ct); return; }

        var resolvedName = await ResolveDisplayNameAsync(request.SprintId, user.GivenName, ct);

        var participant = Participant.JoinWithGoogle(resolvedName, request.SprintId, userId);
        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await SendAsync(participant.Adapt<JoinSprintResponse>(), 201, ct);
    }

    private async Task<string> ResolveDisplayNameAsync(int sprintId, string baseName, CancellationToken ct)
    {
        const int maxLength = 50;
        var truncated = baseName.Length > maxLength ? baseName[..maxLength] : baseName;

        if (!await participantRepository.ExistsByDisplayNameAsync(sprintId, truncated, ct))
            return truncated;

        for (var suffix = 2; suffix <= 100; suffix++)
        {
            var suffixStr = suffix.ToString();
            var maxBase = maxLength - suffixStr.Length - 1; // -1 for the space
            var nameBase = truncated.Length > maxBase ? truncated[..maxBase] : truncated;
            var candidate = $"{nameBase} {suffixStr}";

            if (!await participantRepository.ExistsByDisplayNameAsync(sprintId, candidate, ct))
                return candidate;
        }

        ThrowError("Unable to assign a unique display name. Too many participants with the same given name.", StatusCodes.Status409Conflict);
        return default!; // unreachable — ThrowError throws
    }
}

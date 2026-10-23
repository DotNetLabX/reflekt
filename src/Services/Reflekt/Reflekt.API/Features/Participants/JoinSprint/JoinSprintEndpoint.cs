using Blocks.Core;
using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.Domain.Participants;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Participants.JoinSprint;

[AllowAnonymous]
[HttpPost("/api/sprints/{sprintId}/join")]
[Tags("Participants")]
public sealed class JoinSprintEndpoint(SprintRepository sprintRepository, ParticipantRepository participantRepository)
    : Endpoint<JoinSprintCommand, JoinSprintResponse>
{
    public override async Task HandleAsync(JoinSprintCommand command, CancellationToken ct)
    {
        var sprint = Guard.NotFound(await sprintRepository.GetByIdAsync(command.SprintId, ct));
        sprint.EnsureActive("Sprint is not accepting participants.");

        if (await participantRepository.ExistsByDisplayNameAsync(command.SprintId, command.DisplayName, ct))
            throw new ConflictException("Name already taken.");

        var participant = Participant.Join(command.DisplayName, command.SprintId);
        await participantRepository.AddAsync(participant, ct);
        await participantRepository.SaveChangesAsync(ct);

        await SendAsync(participant.Adapt<JoinSprintResponse>(), 201, ct);
    }
}

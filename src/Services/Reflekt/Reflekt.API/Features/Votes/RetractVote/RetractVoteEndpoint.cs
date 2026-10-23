using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.RetractVote;

[Authorize(Policy = Auth.Policies.Guest)]
[HttpDelete("/api/sprints/{sprintId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class RetractVoteEndpoint(
    SprintAccess sprintAccess,
    VoteRepository voteRepository,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<RetractVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(RetractVoteRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        access.RequireGuestOf(req.SprintId);
        var participantId = access.RequireParticipant();

        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.RetractVote);

        var vote = await voteRepository.GetOneByParticipantAndGroupAsync(participantId, req.GroupId, ct)
            ?? throw new ConflictException("No votes to retract on this group.");

        vote.MarkRetracted();
        voteRepository.Remove(vote);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByGroupAsync(req.GroupId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndGroupAsync(participantId, req.GroupId, ct);

        await SendOkAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), ct);
    }
}

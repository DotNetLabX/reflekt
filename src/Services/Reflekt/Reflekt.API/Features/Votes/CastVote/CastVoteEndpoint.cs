using Blocks.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Votes;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.CastVote;

[Authorize(Policy = Auth.Policies.Guest)]
[HttpPost("/api/sprints/{sprintId}/card-groups/{groupId}/votes")]
[Tags("Votes")]
public sealed class CastVoteEndpoint(
    SprintAccess sprintAccess,
    CardGroupRepository cardGroupRepository,
    VoteRepository voteRepository,
    VoteBudget voteBudget,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<CastVoteRequest, VoteResponse>
{
    public override async Task HandleAsync(CastVoteRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        access.RequireGuestOf(req.SprintId);
        var participantId = access.RequireParticipant();

        var group = await cardGroupRepository.GetByIdAsync(req.GroupId, ct);
        if (group is null || group.SprintId != req.SprintId)
            throw new NotFoundException("CardGroup not found");

        var sprint = access.RequireSprint();
        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.CastVote);

        await using var tx = await voteRepository.BeginSerializableAsync(ct);

        await voteBudget.EnsureCanCastAsync(sprint, participantId, ct);

        var existingVote = await voteRepository.GetOneByParticipantAndGroupAsync(participantId, req.GroupId, ct);
        if (existingVote is not null)
            throw new ConflictException("Already voted on this group.");

        var vote = Vote.Create(req.GroupId, participantId);
        await voteRepository.AddAsync(vote, ct);
        await voteRepository.SaveChangesAsync(ct);

        var voteCount = await voteRepository.CountByGroupAsync(req.GroupId, ct);
        var myVoteCount = await voteRepository.CountByParticipantAndGroupAsync(participantId, req.GroupId, ct);

        await tx.CommitAsync(ct);

        await SendAsync(new VoteResponse(req.GroupId, voteCount, myVoteCount), 201, ct);
    }
}

using Blocks.Domain;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.CastVote;

public sealed class VoteBudget(VoteRepository voteRepository)
{
    public async Task EnsureCanCastAsync(Sprint sprint, int participantId, CancellationToken ct)
    {
        var usedVotes = await voteRepository.CountByParticipantAsync(participantId, ct);
        if (usedVotes >= sprint.VotesPerParticipant)
            throw new DomainException("Vote budget exhausted.");
    }
}

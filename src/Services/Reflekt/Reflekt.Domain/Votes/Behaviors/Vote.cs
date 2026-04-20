using Reflekt.Domain.Votes.Events;

namespace Reflekt.Domain.Votes;

public sealed partial class Vote
{
    public static Vote Create(int cardId, int participantId)
    {
        var vote = new Vote
        {
            CardId = cardId,
            ParticipantId = participantId,
            CreatedAt = DateTime.UtcNow,
        };
        vote.AddDomainEvent(new VoteCastEvent(vote));
        return vote;
    }

    public void MarkRetracted()
    {
        AddDomainEvent(new VoteRetractedEvent(this));
    }
}

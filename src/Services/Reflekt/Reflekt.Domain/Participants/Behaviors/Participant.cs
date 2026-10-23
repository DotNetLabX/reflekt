namespace Reflekt.Domain.Participants;

public sealed partial class Participant
{
    public static Participant Join(string displayName, int sprintId)
        => NewParticipant(displayName, sprintId, userId: null);

    public static Participant JoinWithGoogle(string displayName, int sprintId, int userId)
        => NewParticipant(displayName, sprintId, userId);

    private static Participant NewParticipant(string displayName, int sprintId, int? userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var participant = new Participant
        {
            SprintId = sprintId,
            DisplayName = displayName,
            GuestToken = Guid.NewGuid().ToString(),
            JoinedAt = DateTime.UtcNow,
            UserId = userId,
        };
        participant.AddDomainEvent(new Events.ParticipantJoinedEvent(participant));
        return participant;
    }

    public void Kick()
    {
        if (IsRemoved)
            return;

        RemovedAt = DateTime.UtcNow;
        AddDomainEvent(new Events.ParticipantKickedEvent(this));
    }
}

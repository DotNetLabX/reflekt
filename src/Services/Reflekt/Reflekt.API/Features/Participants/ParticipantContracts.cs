using Reflekt.Domain.Participants;

namespace Reflekt.API.Features.Participants;

public sealed record JoinSprintResponse(int Id, int SprintId, string DisplayName, string GuestToken, DateTime JoinedAt);

public sealed record ParticipantResponse(int Id, int SprintId, string DisplayName, DateTime JoinedAt);

public static class ParticipantMappings
{
    public static JoinSprintResponse ToJoinResponse(Participant p) => new(p.Id, p.SprintId, p.DisplayName, p.GuestToken, p.JoinedAt);
    public static ParticipantResponse ToResponse(Participant p) => new(p.Id, p.SprintId, p.DisplayName, p.JoinedAt);
}

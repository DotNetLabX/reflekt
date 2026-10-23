namespace Reflekt.API.Features.EvidenceLinks.DetachEvidenceLink;

public sealed record DetachEvidenceLinkRequest(int SprintId, int CardId, int EvidenceLinkId);

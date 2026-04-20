namespace Reflekt.API.Features.Votes;

public sealed record VoteResponse(int CardId, int VoteCount, int MyVoteCount);

public sealed record VoteDistributionEntry(int CardId, int Count);

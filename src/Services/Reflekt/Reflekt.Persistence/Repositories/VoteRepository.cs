using Microsoft.EntityFrameworkCore;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence.Repositories;

public sealed class VoteRepository(ReflektDbContext db) : Repository<Vote>(db)
{
    public Task<int> CountByParticipantAsync(int participantId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.ParticipantId == participantId, ct);

    public Task<int> CountByCardAsync(int cardId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.CardId == cardId, ct);

    public Task<int> CountByParticipantAndCardAsync(int participantId, int cardId, CancellationToken ct)
        => Db.Votes.CountAsync(v => v.ParticipantId == participantId && v.CardId == cardId, ct);

    public Task<Vote?> GetOneByParticipantAndCardAsync(int participantId, int cardId, CancellationToken ct)
        => Db.Votes.FirstOrDefaultAsync(v => v.ParticipantId == participantId && v.CardId == cardId, ct);

    public Task<List<VoteDistributionEntry>> GetVoteDistributionAsync(int participantId, CancellationToken ct)
        => Db.Votes
            .Where(v => v.ParticipantId == participantId)
            .GroupBy(v => v.CardId)
            .Select(g => new VoteDistributionEntry(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<Dictionary<int, int>> GetVoteCountsByCardIdsAsync(IEnumerable<int> cardIds, CancellationToken ct)
    {
        var ids = cardIds.ToList();
        return await Db.Votes
            .Where(v => ids.Contains(v.CardId))
            .GroupBy(v => v.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CardId, x => x.Count, ct);
    }

    public void Remove(Vote vote) => Db.Votes.Remove(vote);
}

public sealed record VoteDistributionEntry(int CardId, int Count);

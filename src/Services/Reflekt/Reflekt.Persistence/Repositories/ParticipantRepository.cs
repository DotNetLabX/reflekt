using Blocks.Domain;
using Microsoft.Data.Sqlite;
using Reflekt.Domain.Participants;

namespace Reflekt.Persistence.Repositories;

public sealed class ParticipantRepository(ReflektDbContext db) : RepositoryBase<ReflektDbContext, Participant>(db)
{
    private const int SqliteConstraintUnique = 2067;
    private const string DisplayNameIndexColumns = "Participants.SprintId, Participants.DisplayName";

    public Task<Participant?> GetByGuestTokenAsync(string guestToken, CancellationToken ct)
        => Db.Participants
            .SingleOrDefaultAsync(p => p.GuestToken == guestToken, ct);

    public async Task<IReadOnlyList<Participant>> GetBySprintIdAsync(int sprintId, CancellationToken ct)
        => await Db.Participants
            .AsNoTracking()
            .Where(p => p.SprintId == sprintId && p.RemovedAt == null)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);

    public Task<int> CountBySprintIdAsync(int sprintId, CancellationToken ct)
        => Db.Participants.CountAsync(p => p.SprintId == sprintId && p.RemovedAt == null, ct);

    public Task<int> CountWithoutVotesInSprintAsync(int sprintId, CancellationToken ct)
        => Db.Participants.CountAsync(
            p => p.SprintId == sprintId
                && p.RemovedAt == null
                && !Db.Votes.Any(v => v.ParticipantId == p.Id
                    && Db.CardGroups.Any(g => g.Id == v.GroupId && g.SprintId == sprintId)),
            ct);

    public Task<bool> ExistsByDisplayNameAsync(int sprintId, string displayName, CancellationToken ct)
        => Db.Participants
            .AnyAsync(p => p.SprintId == sprintId
                && p.RemovedAt == null
                && p.DisplayName.ToLower() == displayName.ToLower(), ct);

    // Never served from the change tracker: a second check in the same scope must see a kick committed since the first.
    public Task<bool> IsActiveAsync(int participantId, CancellationToken ct)
        => Db.Participants
            .AsNoTracking()
            .AnyAsync(p => p.Id == participantId && p.RemovedAt == null, ct);

    public Task<Participant?> GetByUserAndSprintAsync(int userId, int sprintId, CancellationToken ct)
        => Db.Participants
            .SingleOrDefaultAsync(p => p.UserId == userId && p.SprintId == sprintId, ct);

    public override async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDisplayNameTaken(ex))
        {
            throw new DomainException("Name already taken.", ex);
        }
    }

    private static bool IsDisplayNameTaken(DbUpdateException ex)
        => ex.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode == SqliteConstraintUnique
            && sqlite.Message.Contains(DisplayNameIndexColumns, StringComparison.Ordinal);
}

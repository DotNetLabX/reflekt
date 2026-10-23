using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Users;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence;

internal static class DbInitializer
{
    public static async Task SeedAsync(ReflektDbContext db, SeedOptions seedOptions, CancellationToken ct = default)
    {
        var seedEmail = seedOptions.SeedUserEmail ?? "dev@localhost";

        var user = await EnsureUserAsync(db, seedEmail, ct);
        var team = await EnsureTeamAsync(db, user, ct);
        var sprint = await EnsureSprintAsync(db, team, ct);

        await SeedParticipantsAsync(db, sprint, ct);
        await SeedCardsAsync(db, sprint, ct);
        await SeedGroupsAsync(db, sprint, ct);
        await SeedEvidenceLinksAsync(db, sprint, ct);
        await SeedVotesAsync(db, sprint, ct);
    }

    private static async Task<User> EnsureUserAsync(ReflektDbContext db, string seedEmail, CancellationToken ct)
    {
        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => EF.Functions.Collate(u.Email, "NOCASE") == seedEmail, ct);
        if (existingUser is not null)
            return existingUser;

        var user = User.Create(seedEmail, seedEmail, seedEmail);
        user.ClearDomainEvents();
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Team> EnsureTeamAsync(ReflektDbContext db, User user, CancellationToken ct)
    {
        if (await db.Teams.AnyAsync(ct))
            return await db.Teams.FirstAsync(ct);

        var team = Team.Create("Dev Team", user.Id);
        team.ClearDomainEvents();
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        return team;
    }

    private static async Task<Sprint> EnsureSprintAsync(ReflektDbContext db, Team team, CancellationToken ct)
    {
        if (await db.Sprints.AnyAsync(s => s.TeamId == team.Id, ct))
            return await db.Sprints.FirstAsync(s => s.TeamId == team.Id, ct);

        var sprint = Sprint.Create("Sprint 1", team.Id);
        sprint.Start();
        sprint.ClearDomainEvents();
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync(ct);
        return sprint;
    }

    private static async Task SeedParticipantsAsync(ReflektDbContext db, Sprint sprint, CancellationToken ct)
    {
        if (await db.Participants.AnyAsync(p => p.SprintId == sprint.Id, ct))
            return;

        string[] names = ["Ana", "Mihai", "Elena"];
        foreach (var name in names)
        {
            var participant = Participant.Join(name, sprint.Id);
            participant.ClearDomainEvents();
            db.Participants.Add(participant);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCardsAsync(ReflektDbContext db, Sprint sprint, CancellationToken ct)
    {
        if (await db.Cards.AnyAsync(c => c.SprintId == sprint.Id, ct))
            return;

        var participants = await db.Participants
            .Where(p => p.SprintId == sprint.Id)
            .ToListAsync(ct);

        var cards = new[]
        {
            Card.Create("Great team collaboration this sprint!", sprint.Id, participants[0].Id, ColumnType.Column1, CardType.Note, null),
            Card.Create("CI pipeline was reliable throughout.", sprint.Id, participants[1].Id, ColumnType.Column1, CardType.Note, null),
            Card.Create("Sprint planning took too long.", sprint.Id, participants[2].Id, ColumnType.Column2, CardType.Note, null),
            Card.Create("Communication gaps between front and back.", sprint.Id, participants[0].Id, ColumnType.Column2, CardType.Note, null),
            Card.Create("Update onboarding docs.", sprint.Id, participants[1].Id, ColumnType.ActionItems, CardType.ActionItem, participants[2].DisplayName),
        };

        foreach (var card in cards)
        {
            card.ClearDomainEvents();
            db.Cards.Add(card);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedGroupsAsync(ReflektDbContext db, Sprint sprint, CancellationToken ct)
    {
        if (await db.CardGroups.AnyAsync(g => g.SprintId == sprint.Id, ct))
            return;

        var noteCards = await db.Cards
            .Where(c => c.SprintId == sprint.Id && c.CardType == CardType.Note)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        foreach (var card in noteCards)
        {
            var group = CardGroup.CreateSolo(sprint.Id, card.ColumnType);
            db.CardGroups.Add(group);
            await db.SaveChangesAsync(ct);
            card.SetGroupId(group.Id);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEvidenceLinksAsync(ReflektDbContext db, Sprint sprint, CancellationToken ct)
    {
        var firstWentWellCard = await db.Cards
            .FirstOrDefaultAsync(c => c.SprintId == sprint.Id && c.ColumnType == ColumnType.Column1, ct);

        if (firstWentWellCard is null || await db.EvidenceLinks.AnyAsync(e => e.CardId == firstWentWellCard.Id, ct))
            return;

        firstWentWellCard.AttachEvidenceLink("https://github.com/dotnet/aspnetcore/pull/12345", EvidenceLinkSource.GitHub, null);
        firstWentWellCard.ClearDomainEvents();
        firstWentWellCard.AttachEvidenceLink("https://learn.microsoft.com/en-us/aspnet/core", EvidenceLinkSource.Generic, null);
        firstWentWellCard.ClearDomainEvents();
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedVotesAsync(ReflektDbContext db, Sprint sprint, CancellationToken ct)
    {
        if (await db.Votes.AnyAsync(ct))
            return;

        var participants = await db.Participants
            .Where(p => p.SprintId == sprint.Id)
            .ToListAsync(ct);

        var cards = await db.Cards
            .Where(c => c.SprintId == sprint.Id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var wentWellCards = cards.Where(c => c.ColumnType == ColumnType.Column1 && c.GroupId.HasValue).ToList();
        var didntGoWellCards = cards.Where(c => c.ColumnType == ColumnType.Column2 && c.GroupId.HasValue).ToList();

        if (wentWellCards.Count == 0 || didntGoWellCards.Count == 0)
            return;

        var votesA = new[]
        {
            Vote.Create(wentWellCards[0].GroupId!.Value, participants[0].Id),
            Vote.Create(didntGoWellCards[0].GroupId!.Value, participants[0].Id),
        };

        var votesB = new[]
        {
            Vote.Create(wentWellCards[0].GroupId!.Value, participants[1].Id),
            Vote.Create(didntGoWellCards[0].GroupId!.Value, participants[1].Id),
        };

        foreach (var vote in votesA.Concat(votesB))
        {
            vote.ClearDomainEvents();
            db.Votes.Add(vote);
        }
        await db.SaveChangesAsync(ct);
    }
}

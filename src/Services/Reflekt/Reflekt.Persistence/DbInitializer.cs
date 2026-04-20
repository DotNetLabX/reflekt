using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence;

internal static class DbInitializer
{
    public static async Task SeedAsync(ReflektDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        var allowedEmails = config.GetSection("Auth:AllowedEmails").Get<List<string>>();
        if (allowedEmails is not { Count: > 0 })
            return;

        Team team;
        if (!await db.Teams.AnyAsync(ct))
        {
            team = Team.Create("Dev Team", allowedEmails[0]);
            team.ClearDomainEvents();
            db.Teams.Add(team);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            team = await db.Teams.FirstAsync(ct);
        }

        Sprint sprint;
        if (!await db.Sprints.AnyAsync(s => s.TeamId == team.Id, ct))
        {
            sprint = Sprint.Create("Sprint 1", team.Id);
            sprint.Start();
            sprint.ClearDomainEvents();
            db.Sprints.Add(sprint);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            sprint = await db.Sprints.FirstAsync(s => s.TeamId == team.Id, ct);
        }

        if (!await db.Participants.AnyAsync(p => p.SprintId == sprint.Id, ct))
        {
            string[] names = ["Ana", "Mihai", "Elena"];
            foreach (var name in names)
            {
                var participant = Participant.Join(name, sprint.Id);
                participant.ClearDomainEvents();
                db.Participants.Add(participant);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Cards.AnyAsync(c => c.SprintId == sprint.Id, ct))
        {
            var participants = await db.Participants
                .Where(p => p.SprintId == sprint.Id)
                .ToListAsync(ct);

            var cards = new[]
            {
                Card.Create("Great team collaboration this sprint!", sprint.Id, participants[0].Id, ColumnType.WentWell, CardType.Note, null),
                Card.Create("CI pipeline was reliable throughout.", sprint.Id, participants[1].Id, ColumnType.WentWell, CardType.Note, null),
                Card.Create("Sprint planning took too long.", sprint.Id, participants[2].Id, ColumnType.DidntGoWell, CardType.Note, null),
                Card.Create("Communication gaps between front and back.", sprint.Id, participants[0].Id, ColumnType.DidntGoWell, CardType.Note, null),
                Card.Create("Update onboarding docs.", sprint.Id, participants[1].Id, ColumnType.ActionItems, CardType.ActionItem, participants[2].DisplayName),
            };

            foreach (var card in cards)
            {
                card.ClearDomainEvents();
                db.Cards.Add(card);
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Votes.AnyAsync(ct))
        {
            var participants = await db.Participants
                .Where(p => p.SprintId == sprint.Id)
                .ToListAsync(ct);

            var cards = await db.Cards
                .Where(c => c.SprintId == sprint.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

            var wentWellCards = cards.Where(c => c.ColumnType == ColumnType.WentWell).ToList();
            var didntGoWellCards = cards.Where(c => c.ColumnType == ColumnType.DidntGoWell).ToList();

            if (wentWellCards.Count > 0 && didntGoWellCards.Count > 0)
            {
                // Participant A: 2 votes on first WentWell, 1 vote on first DidntGoWell (full budget of 3)
                var votesA = new[]
                {
                    Vote.Create(wentWellCards[0].Id, participants[0].Id),
                    Vote.Create(wentWellCards[0].Id, participants[0].Id),
                    Vote.Create(didntGoWellCards[0].Id, participants[0].Id),
                };

                // Participant B: 1 vote on first WentWell, 1 vote on first DidntGoWell (2 of 3)
                var votesB = new[]
                {
                    Vote.Create(wentWellCards[0].Id, participants[1].Id),
                    Vote.Create(didntGoWellCards[0].Id, participants[1].Id),
                };

                foreach (var vote in votesA.Concat(votesB))
                {
                    vote.ClearDomainEvents();
                    db.Votes.Add(vote);
                }
                await db.SaveChangesAsync(ct);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;

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
    }
}

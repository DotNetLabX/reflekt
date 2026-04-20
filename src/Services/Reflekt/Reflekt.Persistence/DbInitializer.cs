using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        if (!await db.Sprints.AnyAsync(s => s.TeamId == team.Id, ct))
        {
            var sprint = Sprint.Create("Sprint 1", team.Id);
            sprint.ClearDomainEvents();
            db.Sprints.Add(sprint);
            await db.SaveChangesAsync(ct);
        }
    }
}

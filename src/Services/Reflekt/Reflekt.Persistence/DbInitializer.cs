using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence;

internal static class DbInitializer
{
    public static async Task SeedAsync(ReflektDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        var allowedEmails = config.GetSection("Auth:AllowedEmails").Get<List<string>>();
        if (allowedEmails is not { Count: > 0 })
            return;

        if (!await db.Teams.AnyAsync(ct))
        {
            var team = Team.Create("Dev Team", allowedEmails[0]);
            team.ClearDomainEvents(); // seed is infrastructure — no event dispatch before app is ready
            db.Teams.Add(team);
            await db.SaveChangesAsync(ct);
        }
    }
}

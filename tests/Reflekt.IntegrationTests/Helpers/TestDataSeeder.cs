using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Users;
using Reflekt.Persistence;

namespace Reflekt.IntegrationTests.Helpers;

public static class TestDataSeeder
{
    public static async Task SeedTestUserAsync(this WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();

        if (await db.Set<User>().AnyAsync(u => u.Id == TestAuthHandler.TestUserId))
            return;

        db.Add(User.Create(TestAuthHandler.TestEmail, TestAuthHandler.TestDisplayName, TestAuthHandler.TestDisplayName));
        await db.SaveChangesAsync();

        var seeded = await db.Set<User>().SingleAsync(u => u.Email == TestAuthHandler.TestEmail);
        if (seeded.Id != TestAuthHandler.TestUserId)
            throw new InvalidOperationException($"Test user seeded with id {seeded.Id}, expected {TestAuthHandler.TestUserId}.");
    }

    public static async Task SeedDiscussedGroupsAsync(this WebApplicationFactory<Program> factory, int sprintId, int count = 2)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReflektDbContext>();

        for (var i = 0; i < count; i++)
        {
            var group = CardGroup.Create(sprintId, ColumnType.Column1);
            group.MarkDiscussed();
            group.ClearDomainEvents();
            db.Add(group);
        }

        await db.SaveChangesAsync();
    }

    public static string CreateScrumMasterCookie(this WebApplicationFactory<Program> factory)
    {
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, TestAuthHandler.TestUserId.ToString()),
                new Claim(ClaimTypes.Email, TestAuthHandler.TestEmail),
                new Claim(ClaimTypes.Name, TestAuthHandler.TestDisplayName),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity), CookieAuthenticationDefaults.AuthenticationScheme);

        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }
}

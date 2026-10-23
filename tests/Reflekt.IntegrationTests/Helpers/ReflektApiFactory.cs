using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reflekt.API.Auth;

namespace Reflekt.IntegrationTests.Helpers;

public static class ReflektApiFactory
{
    public static WebApplicationFactory<Program> CreateAnonymous(string dbPath) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseTestDatabase(dbPath));

    public static WebApplicationFactory<Program> CreateSignedIn(string dbPath, params string[] testUserPolicies) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseTestDatabase(dbPath);
                builder.ConfigureTestServices(services =>
                {
                    services
                        .AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });

                    var authorization = services.AddAuthorizationBuilder();
                    foreach (var policyName in testUserPolicies)
                        authorization.AddPolicy(policyName, policy => policy
                            .AddAuthenticationSchemes(SchemesFor(policyName))
                            .RequireAuthenticatedUser());
                });
            });

    public static async Task DeleteDatabaseFilesAsync(params string[] dbPaths)
    {
        SqliteConnection.ClearAllPools();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                foreach (var dbPath in dbPaths)
                    if (File.Exists(dbPath)) File.Delete(dbPath);
                break;
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(100);
                SqliteConnection.ClearAllPools();
            }
        }
    }

    private static IWebHostBuilder UseTestDatabase(this IWebHostBuilder builder, string dbPath) =>
        builder
            .UsePlaceholderGoogleCredentials()
            .ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Reflekt"] = $"Data Source={dbPath}",
                    ["Seed:Enabled"] = "false",
                }));

    private static string[] SchemesFor(string policyName) => policyName switch
    {
        Policies.SignedInUser => [TestAuthHandler.SchemeName],
        Policies.SignedInUserOrGuest => [TestAuthHandler.SchemeName, GuestTokenDefaults.AuthenticationScheme],
        Policies.Guest => [GuestTokenDefaults.AuthenticationScheme],
        _ => throw new ArgumentOutOfRangeException(nameof(policyName), policyName, "No test scheme set for this policy."),
    };
}

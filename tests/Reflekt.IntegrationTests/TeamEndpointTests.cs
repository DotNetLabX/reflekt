using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reflekt.API.Auth;
using Reflekt.IntegrationTests.Helpers;

namespace Reflekt.IntegrationTests;

public class TeamEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-reflekt-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-reflekt-{Guid.NewGuid():N}.db");

    private WebApplicationFactory<Program> _authFactory = null!;
    private WebApplicationFactory<Program> _anonFactory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;

    public async Task InitializeAsync()
    {
        _authFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UsePlaceholderGoogleCredentials();
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Reflekt"] = $"Data Source={_testDbPath}",
                        ["Auth:AllowedEmails:0"] = TestAuthHandler.TestEmail,
                        ["Seed:Enabled"] = "false",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services
                        .AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });

                    services.AddAuthorizationBuilder()
                        .AddPolicy(Policies.ScrumMaster, policy => policy
                            .AddAuthenticationSchemes(TestAuthHandler.SchemeName)
                            .RequireAuthenticatedUser());
                });
            });

        _anonFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UsePlaceholderGoogleCredentials();
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Reflekt"] = $"Data Source={_anonDbPath}",
                        ["Seed:Enabled"] = "false",
                    }));
            });

        _client = _authFactory.CreateClient();
        _anonClient = _anonFactory.CreateClient();
        await _authFactory.SeedTestUserAsync();
    }

    [Fact]
    public async Task CreateTeam_Authenticated_ReturnsCreatedTeam()
    {
        var response = await _client.PostAsJsonAsync("/api/teams", new { name = "My Team" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("name").GetString().Should().Be("My Team");
        body.GetProperty("createdAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTeam_Unauthenticated_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/teams", new { name = "My Team" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_EmptyName_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/teams", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyTeams_Authenticated_ReturnsTeams()
    {
        await _client.PostAsJsonAsync("/api/teams", new { name = "My Team" });

        var response = await _client.GetAsync("/api/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        teams.Should().HaveCount(1);
        teams![0].GetProperty("name").GetString().Should().Be("My Team");
    }

    [Fact]
    public async Task GetMyTeams_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/teams");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTeams_NoTeams_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        teams.Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        await _authFactory.DisposeAsync();
        await _anonFactory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
                if (File.Exists(_anonDbPath)) File.Delete(_anonDbPath);
                break;
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(100);
                SqliteConnection.ClearAllPools();
            }
        }
    }
}

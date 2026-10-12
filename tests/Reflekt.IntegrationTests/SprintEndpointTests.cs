using System.Net;
using System.Net.Http.Json;
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

public class SprintEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-sprint-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-sprint-anon-{Guid.NewGuid():N}.db");

    private WebApplicationFactory<Program> _authFactory = null!;
    private WebApplicationFactory<Program> _anonFactory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;
    private int _teamId;

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

        var res = await _client.PostAsJsonAsync("/api/teams", new { name = "Test Team" });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        _teamId = body.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CreateSprint_ReturnsCreated()
    {
        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints", new { name = "Sprint 1" });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("name").GetString().Should().Be("Sprint 1");
        body.GetProperty("state").GetString().Should().Be("Draft");
        body.GetProperty("currentPhase").GetString().Should().Be("Writing");
    }

    [Fact]
    public async Task CreateSprint_EmptyName_Returns400()
    {
        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints", new { name = "" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSprint_ZeroVotes_Returns400()
    {
        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints",
            new { name = "Sprint 1", votesPerParticipant = 0 });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSprint_WrongTeam_Returns404()
    {
        var res = await _client.PostAsJsonAsync("/api/teams/9999/sprints", new { name = "Sprint 1" });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSprint_Unauthenticated_Returns401()
    {
        var res = await _anonClient.PostAsJsonAsync($"/api/teams/{_teamId}/sprints", new { name = "Sprint 1" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartSprint_FromDraft_Returns200()
    {
        var sprintId = await CreateSprintAsync("Start Test");

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("state").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task StartSprint_FromActive_Returns409()
    {
        var sprintId = await CreateSprintAsync("Start Conflict Test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdvancePhase_FullCycle_Returns200()
    {
        var sprintId = await CreateSprintAsync("Phase Cycle Test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });

        var res1 = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentPhase").GetString().Should().Be("Grouping");

        var res2 = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentPhase").GetString().Should().Be("Voting");

        var res3 = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res3.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res3.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentPhase").GetString().Should().Be("Discussing");
        await _authFactory.SeedDiscussedGroupsAsync(sprintId);

        var res4 = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res4.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res4.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("currentPhase").GetString().Should().Be("Reporting");
    }

    [Fact]
    public async Task AdvancePhase_OnReportingPhase_Returns409()
    {
        var sprintId = await CreateSprintAsync("Advance Reporting Test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        await _authFactory.SeedDiscussedGroupsAsync(sprintId);
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdvancePhase_OnDraftSprint_Returns409()
    {
        var sprintId = await CreateSprintAsync("Advance Draft Test");

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CloseSprint_FromActive_Returns200()
    {
        var sprintId = await CreateSprintAsync("Close Test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("state").GetString().Should().Be("Closed");
        body.GetProperty("closedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CloseSprint_FromDraft_Returns409()
    {
        var sprintId = await CreateSprintAsync("Close Draft Test");

        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetSprint_Returns200()
    {
        var sprintId = await CreateSprintAsync("Get Test");

        var res = await _client.GetAsync($"/api/teams/{_teamId}/sprints/{sprintId}");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(sprintId);
        body.GetProperty("name").GetString().Should().Be("Get Test");
    }

    [Fact]
    public async Task GetSprint_WrongTeam_Returns404()
    {
        var sprintId = await CreateSprintAsync("Wrong Team Test");

        var res = await _client.GetAsync($"/api/teams/9999/sprints/{sprintId}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeamSprints_ReturnsList()
    {
        await CreateSprintAsync("List Sprint A");
        await CreateSprintAsync("List Sprint B");

        var res = await _client.GetAsync($"/api/teams/{_teamId}/sprints");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetTeamSprints_EmptyTeam_ReturnsEmptyArray()
    {
        var emptyDbPath = Path.Combine(Path.GetTempPath(), $"test-sprint-empty-{Guid.NewGuid():N}.db");
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UsePlaceholderGoogleCredentials();
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Reflekt"] = $"Data Source={emptyDbPath}",
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

        var client = factory.CreateClient();
        await factory.SeedTestUserAsync();
        var teamRes = await client.PostAsJsonAsync("/api/teams", new { name = "Empty Team" });
        var teamBody = await teamRes.Content.ReadFromJsonAsync<JsonElement>();
        var teamId = teamBody.GetProperty("id").GetInt32();

        var res = await client.GetAsync($"/api/teams/{teamId}/sprints");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var sprints = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        sprints.Should().BeEmpty();

        SqliteConnection.ClearAllPools();
        if (File.Exists(emptyDbPath)) File.Delete(emptyDbPath);
    }

    public async Task DisposeAsync()
    {
        await _authFactory.DisposeAsync();
        await _anonFactory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        if (File.Exists(_anonDbPath)) File.Delete(_anonDbPath);
    }

    private async Task<int> CreateSprintAsync(string name)
    {
        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints", new { name });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }
}

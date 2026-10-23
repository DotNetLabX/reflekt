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
using Reflekt.API.Auth;
using Reflekt.IntegrationTests.Helpers;

namespace Reflekt.IntegrationTests;

public class ParticipantEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-participant-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-participant-anon-{Guid.NewGuid():N}.db");

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
                            .RequireAuthenticatedUser())
                        .AddPolicy(Policies.OwnerOrScrumMaster, policy => policy
                            .AddAuthenticationSchemes(
                                TestAuthHandler.SchemeName,
                                GuestTokenDefaults.AuthenticationScheme)
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
    public async Task JoinSprint_ActiveSprint_Returns201()
    {
        var sprintId = await CreateActiveSprintAsync("Join Test");

        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join", new { displayName = "Ana" });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("sprintId").GetInt32().Should().Be(sprintId);
        body.GetProperty("displayName").GetString().Should().Be("Ana");
        body.GetProperty("guestToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("joinedAt").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task JoinSprint_EmptyDisplayName_Returns400()
    {
        var sprintId = await CreateActiveSprintAsync("Empty Name Test");
        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join", new { displayName = "" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinSprint_DisplayNameTooLong_Returns400()
    {
        var sprintId = await CreateActiveSprintAsync("Long Name Test");
        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join",
            new { displayName = new string('A', 51) });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JoinSprint_DraftSprint_Returns409()
    {
        var sprintId = await CreateSprintAsync("Draft Sprint Test");
        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join", new { displayName = "Ana" });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task JoinSprint_ClosedSprint_Returns409()
    {
        var sprintId = await CreateActiveSprintAsync("Closed Sprint Test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join", new { displayName = "Ana" });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task JoinSprint_NonexistentSprint_Returns404()
    {
        var res = await _client.PostAsJsonAsync("/api/sprints/9999/join", new { displayName = "Ana" });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentParticipant_ValidToken_Returns200()
    {
        var sprintId = await CreateActiveSprintAsync("GetMe Test");
        var (_, guestToken) = await JoinSprintAsync(sprintId, "Ana");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/participants/me");
        req.Headers.Add("X-Guest-Token", guestToken);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("displayName").GetString().Should().Be("Ana");
        body.GetProperty("sprintId").GetInt32().Should().Be(sprintId);
    }

    [Fact]
    public async Task GetCurrentParticipant_NoToken_Returns401()
    {
        var res = await _anonClient.GetAsync("/api/sprints/1/participants/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentParticipant_InvalidToken_Returns401()
    {
        var sprintId = await CreateActiveSprintAsync("Invalid Token Test");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/participants/me");
        req.Headers.Add("X-Guest-Token", Guid.NewGuid().ToString());
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentParticipant_WrongSprint_Returns401()
    {
        var sprintA = await CreateActiveSprintAsync("Sprint A");
        var sprintB = await CreateActiveSprintAsync("Sprint B");
        var (_, tokenA) = await JoinSprintAsync(sprintA, "Ana");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintB}/participants/me");
        req.Headers.Add("X-Guest-Token", tokenA);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetParticipants_WithGuestToken_ReturnsList()
    {
        var sprintId = await CreateActiveSprintAsync("List Test");
        var (_, token1) = await JoinSprintAsync(sprintId, "Ana");
        await JoinSprintAsync(sprintId, "Mihai");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/participants");
        req.Headers.Add("X-Guest-Token", token1);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetParticipants_WithSmCookie_ReturnsList()
    {
        var sprintId = await CreateActiveSprintAsync("SM List Test");
        await JoinSprintAsync(sprintId, "Ana");

        var res = await _client.GetAsync($"/api/sprints/{sprintId}/participants");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetParticipants_NoAuth_Returns401()
    {
        var res = await _anonClient.GetAsync("/api/sprints/1/participants");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GuestTokenAuth_SetsCorrectClaims()
    {
        var sprintId = await CreateActiveSprintAsync("Claims Test");
        var (participantId, guestToken) = await JoinSprintAsync(sprintId, "Elena");

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/participants/me");
        req.Headers.Add("X-Guest-Token", guestToken);
        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(participantId);
        body.GetProperty("displayName").GetString().Should().Be("Elena");
        body.GetProperty("sprintId").GetInt32().Should().Be(sprintId);
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

    private async Task<int> CreateSprintAsync(string name)
    {
        var res = await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints", new { name });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateActiveSprintAsync(string name)
    {
        var sprintId = await CreateSprintAsync(name);
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/start", new { });
        return sprintId;
    }

    private async Task<(int participantId, string guestToken)> JoinSprintAsync(int sprintId, string displayName)
    {
        var res = await _client.PostAsJsonAsync($"/api/sprints/{sprintId}/join", new { displayName });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("id").GetInt32(), body.GetProperty("guestToken").GetString()!);
    }
}

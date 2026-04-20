using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reflekt.API.Auth;
using Reflekt.IntegrationTests.Helpers;

namespace Reflekt.IntegrationTests;

public class EvidenceLinkEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-evidencelink-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-evidencelink-anon-{Guid.NewGuid():N}.db");

    private WebApplicationFactory<Program> _factory = null!;
    private WebApplicationFactory<Program> _anonFactory = null!;
    private HttpClient _client = null!;
    private HttpClient _anonClient = null!;
    private int _teamId;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
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
                            .RequireAuthenticatedUser())
                        .AddPolicy(Policies.Participant, policy => policy
                            .AddAuthenticationSchemes(GuestTokenDefaults.AuthenticationScheme)
                            .RequireAuthenticatedUser());
                });
            });

        _anonFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Reflekt"] = $"Data Source={_anonDbPath}",
                        ["Seed:Enabled"] = "false",
                    }));
            });

        _client = _factory.CreateClient();
        _anonClient = _anonFactory.CreateClient();

        var res = await _client.PostAsJsonAsync("/api/teams", new { name = "EvidenceLink Test Team" });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        _teamId = body.GetProperty("id").GetInt32();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _anonFactory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        if (File.Exists(_anonDbPath)) File.Delete(_anonDbPath);
    }

    // --- AttachEvidenceLink ---

    [Fact]
    public async Task AttachLink_GitHubUrl_Returns201()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "https://github.com/dotnet/aspnetcore");

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("source").GetString().Should().Be("GitHub");
        body.GetProperty("fetchStatus").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task AttachLink_JiraUrl_Returns201_WithExternalKey()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "https://myorg.atlassian.net/browse/PROJ-123");

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("source").GetString().Should().Be("Jira");
        body.GetProperty("externalKey").GetString().Should().Be("PROJ-123");
        body.GetProperty("fetchStatus").GetString().Should().Be("Ok");
    }

    [Fact]
    public async Task AttachLink_GenericUrl_Returns201()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "https://example.com/page");

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("source").GetString().Should().Be("Generic");
        body.GetProperty("fetchStatus").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task AttachLink_EmptyUrl_Returns400()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AttachLink_UrlExceeds2048_Returns400()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "https://example.com/" + new string('a', 2040));

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AttachLink_NonHttpScheme_Returns400()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, cardId, "ftp://example.com/file.txt");

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AttachLink_NoAuth_Returns401()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await _anonClient.PostAsJsonAsync(
            $"/api/sprints/{sprintId}/cards/{cardId}/evidence-links",
            new { url = "https://github.com/test" });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AttachLink_NotCardOwnerOrSm_Returns403()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");
        var (_, tokenB) = await JoinSprintAsync(sprintId, "Mihai");

        var res = await PostLink(tokenB, sprintId, cardId, "https://github.com/test");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AttachLink_NonexistentCard_Returns404()
    {
        var (sprintId, token, _) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token, sprintId, 9999, "https://github.com/test");

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AttachLink_ClosedSprint_Returns409()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await PostLink(token, sprintId, cardId, "https://github.com/test");

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AttachLink_ByScrumMaster_Returns201()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PostLink(token: null, sprintId, cardId, "https://github.com/test", asScrumMaster: true);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // --- DetachEvidenceLink ---

    [Fact]
    public async Task DetachLink_ByAuthor_Returns204()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        var linkId = await AttachLinkAndGetIdAsync(token, sprintId, cardId, "https://github.com/test");

        var res = await DeleteLink(token, sprintId, cardId, linkId);

        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DetachLink_ByScrumMaster_Returns204()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        var linkId = await AttachLinkAndGetIdAsync(token, sprintId, cardId, "https://github.com/test");

        var res = await DeleteLink(token: null, sprintId, cardId, linkId, asScrumMaster: true);

        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DetachLink_NotOwnerOrSm_Returns403()
    {
        var (sprintId, tokenA, cardId) = await SetupSprintWithCardAsync("Ana");
        var linkId = await AttachLinkAndGetIdAsync(tokenA, sprintId, cardId, "https://github.com/test");
        var (_, tokenB) = await JoinSprintAsync(sprintId, "Mihai");

        var res = await DeleteLink(tokenB, sprintId, cardId, linkId);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DetachLink_NonexistentLink_Returns404()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await DeleteLink(token, sprintId, cardId, 9999);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DetachLink_ClosedSprint_Returns409()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        var linkId = await AttachLinkAndGetIdAsync(token, sprintId, cardId, "https://github.com/test");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await DeleteLink(token, sprintId, cardId, linkId);

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- Card endpoints return evidence links ---

    [Fact]
    public async Task ListCards_IncludesEvidenceLinks()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        await PostLink(token, sprintId, cardId, "https://github.com/dotnet/aspnetcore");

        var cards = await GetCardsAsync(token, sprintId);
        var card = cards.EnumerateArray().First(c => c.GetProperty("id").GetInt32() == cardId);

        card.GetProperty("evidenceLinks").GetArrayLength().Should().Be(1);
        card.GetProperty("evidenceLinks")[0].GetProperty("source").GetString().Should().Be("GitHub");
    }

    [Fact]
    public async Task CreateCard_ReturnsEmptyEvidenceLinks()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");

        var res = await PostCard(token, sprintId, new { text = "New card", columnType = "WentWell", cardType = "Note" });

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("evidenceLinks").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task DeleteCard_CascadeDeletesEvidenceLinks()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        await PostLink(token, sprintId, cardId, "https://github.com/dotnet/aspnetcore");

        await DeleteCard(token, sprintId, cardId);

        var cards = await GetCardsAsync(token, sprintId);
        cards.EnumerateArray().Should().NotContain(c => c.GetProperty("id").GetInt32() == cardId);
    }

    // --- Helpers ---

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

    private async Task<(int sprintId, string token)> SetupActiveSprintWithParticipantAsync(string name)
    {
        var sprintId = await CreateActiveSprintAsync($"Sprint-{Guid.NewGuid():N}");
        var (_, token) = await JoinSprintAsync(sprintId, name);
        return (sprintId, token);
    }

    private async Task<(int sprintId, string token, int cardId)> SetupSprintWithCardAsync(string name)
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync(name);
        var res = await PostCard(token, sprintId, new { text = "Initial card", columnType = "WentWell", cardType = "Note" });
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (sprintId, token, body.GetProperty("id").GetInt32());
    }

    private async Task<int> AttachLinkAndGetIdAsync(string token, int sprintId, int cardId, string url)
    {
        var res = await PostLink(token, sprintId, cardId, url);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> PostCard(string token, int sprintId, object payload)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/sprints/{sprintId}/cards")
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> DeleteCard(string token, int sprintId, int cardId)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/sprints/{sprintId}/cards/{cardId}");
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> PostLink(string? token, int sprintId, int cardId, string url, bool asScrumMaster = false)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/sprints/{sprintId}/cards/{cardId}/evidence-links")
        {
            Content = JsonContent.Create(new { url }),
        };
        if (token is not null) req.Headers.Add("X-Guest-Token", token);
        return asScrumMaster ? _client.SendAsync(req) : _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> DeleteLink(string? token, int sprintId, int cardId, int evidenceLinkId, bool asScrumMaster = false)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId}");
        if (token is not null) req.Headers.Add("X-Guest-Token", token);
        return asScrumMaster ? _client.SendAsync(req) : _client.SendAsync(req);
    }

    private async Task<JsonElement> GetCardsAsync(string token, int sprintId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/cards");
        req.Headers.Add("X-Guest-Token", token);
        var res = await _client.SendAsync(req);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}

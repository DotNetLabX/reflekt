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

public class CardEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-card-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-card-anon-{Guid.NewGuid():N}.db");

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

        var res = await _client.PostAsJsonAsync("/api/teams", new { name = "Card Test Team" });
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

    // --- CreateCard ---

    [Fact]
    public async Task CreateCard_Note_Returns201()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");

        var res = await PostCard(token, sprintId, new { text = "Great teamwork", columnType = "WentWell", cardType = "Note" });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("sprintId").GetInt32().Should().Be(sprintId);
        body.GetProperty("columnType").GetString().Should().Be("WentWell");
        body.GetProperty("cardType").GetString().Should().Be("Note");
        body.GetProperty("text").GetString().Should().Be("Great teamwork");
    }

    [Fact]
    public async Task CreateCard_ActionItem_Returns201()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");

        var res = await PostCard(token, sprintId, new { text = "Update docs", columnType = "ActionItems", cardType = "ActionItem", assigneeName = "Ana" });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("assigneeName").GetString().Should().Be("Ana");
    }

    [Fact]
    public async Task CreateCard_EmptyText_Returns400()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, sprintId, new { text = "", columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_TextTooLong_Returns400()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, sprintId, new { text = new string('A', 2001), columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_ActionItem_NoAssignee_Returns400()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, sprintId, new { text = "Fix bug", columnType = "ActionItems", cardType = "ActionItem" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_ActionItem_WrongColumn_Returns400()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, sprintId, new { text = "Fix bug", columnType = "WentWell", cardType = "ActionItem", assigneeName = "Ana" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_Note_InActionItemsColumn_Returns400()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, sprintId, new { text = "Nice work", columnType = "ActionItems", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_NoGuestToken_Returns401()
    {
        var res = await _anonClient.PostAsJsonAsync("/api/sprints/1/cards",
            new { text = "Test", columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCard_WrongSprint_Returns401()
    {
        var sprintA = await CreateActiveSprintAsync("Sprint A");
        var sprintB = await CreateActiveSprintAsync("Sprint B");
        var (_, tokenA) = await JoinSprintAsync(sprintA, "Ana");

        var res = await PostCard(tokenA, sprintB, new { text = "Test", columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCard_ClosedSprint_Returns409()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await PostCard(token, sprintId, new { text = "Too late", columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCard_NonexistentSprint_Returns401()
    {
        var (_, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var res = await PostCard(token, 9999, new { text = "Test", columnType = "WentWell", cardType = "Note" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- EditCard ---

    [Fact]
    public async Task EditCard_ByAuthor_Returns200()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await PutCard(token, sprintId, cardId, new { text = "Updated text" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("text").GetString().Should().Be("Updated text");
    }

    [Fact]
    public async Task EditCard_ByScrumMaster_Returns200()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");

        var res = await _client.PutAsJsonAsync($"/api/sprints/{sprintId}/cards/{cardId}",
            new { text = "SM edited" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EditCard_ByOtherParticipant_Returns403()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");
        var (_, tokenB) = await JoinSprintAsync(sprintId, "Mihai");

        var res = await PutCard(tokenB, sprintId, cardId, new { text = "Sneaky edit" });

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EditCard_ClosedSprint_Returns409()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await PutCard(token, sprintId, cardId, new { text = "Too late" });

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EditCard_NonexistentCard_Returns404()
    {
        var (sprintId, token, _) = await SetupSprintWithCardAsync("Ana");
        var res = await PutCard(token, sprintId, 9999, new { text = "Ghost" });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditCard_UpdatesTimestamp()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");

        var before = await GetCardAsync(token, sprintId, cardId);
        await Task.Delay(10);
        await PutCard(token, sprintId, cardId, new { text = "Changed" });
        var after = await GetCardAsync(token, sprintId, cardId);

        DateTime.Parse(after.GetProperty("updatedAt").GetString()!)
            .Should().BeAfter(DateTime.Parse(before.GetProperty("updatedAt").GetString()!));
    }

    // --- DeleteCard ---

    [Fact]
    public async Task DeleteCard_ByAuthor_Returns204()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        var res = await DeleteCard(token, sprintId, cardId);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCard_ByScrumMaster_Returns204()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");
        var res = await _client.DeleteAsync($"/api/sprints/{sprintId}/cards/{cardId}");
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCard_ByOtherParticipant_Returns403()
    {
        var (sprintId, _, cardId) = await SetupSprintWithCardAsync("Ana");
        var (_, tokenB) = await JoinSprintAsync(sprintId, "Mihai");

        var res = await DeleteCard(tokenB, sprintId, cardId);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCard_ClosedSprint_Returns409()
    {
        var (sprintId, token, cardId) = await SetupSprintWithCardAsync("Ana");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await DeleteCard(token, sprintId, cardId);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCard_NonexistentCard_Returns404()
    {
        var (sprintId, token, _) = await SetupSprintWithCardAsync("Ana");
        var res = await DeleteCard(token, sprintId, 9999);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- ListCards ---

    [Fact]
    public async Task ListCards_ReturnsAllCardsInSprint()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        await PostCard(token, sprintId, new { text = "Card 1", columnType = "WentWell", cardType = "Note" });
        await PostCard(token, sprintId, new { text = "Card 2", columnType = "DidntGoWell", cardType = "Note" });
        await PostCard(token, sprintId, new { text = "Card 3", columnType = "WentWell", cardType = "Note" });

        var res = await GetCardsAsync(token, sprintId);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListCards_OrderedByCreatedAtDescending()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        await PostCard(token, sprintId, new { text = "First", columnType = "WentWell", cardType = "Note" });
        await Task.Delay(10);
        await PostCard(token, sprintId, new { text = "Second", columnType = "WentWell", cardType = "Note" });

        var res = await GetCardsAsync(token, sprintId);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();

        body![0].GetProperty("text").GetString().Should().Be("Second");
        body![1].GetProperty("text").GetString().Should().Be("First");
    }

    [Fact]
    public async Task ListCards_NoAuth_Returns401()
    {
        var res = await _anonClient.GetAsync("/api/sprints/1/cards");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCards_NonexistentSprint_Returns404()
    {
        var res = await _client.GetAsync("/api/sprints/9999/cards");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private Task<HttpResponseMessage> PostCard(string token, int sprintId, object payload)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/sprints/{sprintId}/cards")
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> PutCard(string token, int sprintId, int cardId, object payload)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/sprints/{sprintId}/cards/{cardId}")
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

    private Task<HttpResponseMessage> GetCardsAsync(string token, int sprintId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/cards");
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private async Task<JsonElement> GetCardAsync(string token, int sprintId, int cardId)
    {
        var res = await GetCardsAsync(token, sprintId);
        var cards = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        return cards!.First(c => c.GetProperty("id").GetInt32() == cardId);
    }
}

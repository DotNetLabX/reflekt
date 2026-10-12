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

public class VoteEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-vote-{Guid.NewGuid():N}.db");

    private readonly string _anonDbPath =
        Path.Combine(Path.GetTempPath(), $"test-vote-anon-{Guid.NewGuid():N}.db");

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
                            .RequireAuthenticatedUser())
                        .AddPolicy(Policies.Participant, policy => policy
                            .AddAuthenticationSchemes(GuestTokenDefaults.AuthenticationScheme)
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

        _client = _factory.CreateClient();
        _anonClient = _anonFactory.CreateClient();
        await _factory.SeedTestUserAsync();

        var res = await _client.PostAsJsonAsync("/api/teams", new { name = "Vote Test Team" });
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

    // --- CastVote ---

    [Fact]
    public async Task CastVote_Returns201()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");

        var res = await PostVote(token, sprintId, groupId);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("groupId").GetInt32().Should().Be(groupId);
        body.GetProperty("voteCount").GetInt32().Should().Be(1);
        body.GetProperty("myVoteCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CastVote_SameGroupTwice_Returns409()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");

        var first = await PostVote(token, sprintId, groupId);
        var res = await PostVote(token, sprintId, groupId);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CastVote_BudgetExhausted_Returns409()
    {
        var (sprintId, token, groupIds) = await SetupSprintWithNoteGroupsAsync("Ana", 4);

        await PostVote(token, sprintId, groupIds[0]);
        await PostVote(token, sprintId, groupIds[1]);
        await PostVote(token, sprintId, groupIds[2]);

        var res = await PostVote(token, sprintId, groupIds[3]);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CastVote_NonexistentGroup_Returns404()
    {
        var (sprintId, token) = await SetupVotingSprintWithParticipantAsync("Ana");

        var res = await PostVote(token, sprintId, 9999);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CastVote_ClosedSprint_Returns409()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await PostVote(token, sprintId, groupId);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CastVote_GroupInDifferentSprint_Returns404()
    {
        var (sprintIdA, tokenA, _) = await SetupSprintWithNoteGroupAsync("Ana");
        var (_, _, groupIdB) = await SetupSprintWithNoteGroupAsync("Mihai");

        var res = await PostVote(tokenA, sprintIdA, groupIdB);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CastVote_NoGuestToken_Returns401()
    {
        var res = await _anonClient.PostAsync("/api/sprints/1/card-groups/1/votes", null);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CastVote_WrongSprint_Returns401()
    {
        var (sprintIdB, _, groupIdB) = await SetupSprintWithNoteGroupAsync("Mihai");
        var (_, tokenA) = await SetupVotingSprintWithParticipantAsync("Ana");

        // tokenA is bound to sprintA, but posting to sprintB — sprint claim mismatch
        var res = await PostVote(tokenA, sprintIdB, groupIdB);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- RetractVote ---

    [Fact]
    public async Task RetractVote_Returns200()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");
        var (_, secondToken) = await JoinSprintAsync(sprintId, "Ben");
        var ownVote = await PostVote(token, sprintId, groupId);
        var otherVote = await PostVote(secondToken, sprintId, groupId);
        ownVote.StatusCode.Should().Be(HttpStatusCode.Created);
        otherVote.StatusCode.Should().Be(HttpStatusCode.Created);

        var res = await DeleteVote(token, sprintId, groupId);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("voteCount").GetInt32().Should().Be(1);
        body.GetProperty("myVoteCount").GetInt32().Should().Be(0);

        var otherVotes = await (await GetMyVotes(secondToken, sprintId)).Content.ReadFromJsonAsync<JsonElement[]>();
        otherVotes.Should().ContainSingle();
        otherVotes![0].GetProperty("groupId").GetInt32().Should().Be(groupId);
        otherVotes[0].GetProperty("count").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task RetractVote_WhenNoVotesOnGroup_Returns409()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");

        var res = await DeleteVote(token, sprintId, groupId);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RetractVote_ClosedSprint_Returns409()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");
        await PostVote(token, sprintId, groupId);
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/close", new { });

        var res = await DeleteVote(token, sprintId, groupId);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RetractVote_ReturnsBudget()
    {
        var (sprintId, token, groupIds) = await SetupSprintWithNoteGroupsAsync("Ana", 3);

        await PostVote(token, sprintId, groupIds[0]);
        await PostVote(token, sprintId, groupIds[1]);
        await PostVote(token, sprintId, groupIds[2]);

        // Budget exhausted — retract one
        await DeleteVote(token, sprintId, groupIds[0]);

        // Now should be able to cast again
        var res = await PostVote(token, sprintId, groupIds[0]);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RetractVote_NoGuestToken_Returns401()
    {
        var res = await _anonClient.DeleteAsync("/api/sprints/1/card-groups/1/votes");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GetMyVotes ---

    [Fact]
    public async Task GetMyVotes_ReturnsDistribution()
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync("Ana");
        var cardId1 = await CreateNoteCardAsync(token, sprintId);
        var cardId2 = await CreateNoteCardAsync(token, sprintId);
        await CreateNoteCardAsync(token, sprintId);

        // Advance to Grouping (auto-creates groups) then to Voting
        await AdvancePhaseAsync(sprintId);
        await AdvancePhaseAsync(sprintId);

        var groupId1 = await GetGroupIdForCardAsync(token, sprintId, cardId1);
        var groupId2 = await GetGroupIdForCardAsync(token, sprintId, cardId2);

        await PostVote(token, sprintId, groupId1);
        await PostVote(token, sprintId, groupId2);

        var res = await GetMyVotes(token, sprintId);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCount(2);
        var group1Entry = body!.First(e => e.GetProperty("groupId").GetInt32() == groupId1);
        group1Entry.GetProperty("count").GetInt32().Should().Be(1);
        var group2Entry = body!.First(e => e.GetProperty("groupId").GetInt32() == groupId2);
        group2Entry.GetProperty("count").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetMyVotes_Empty_ReturnsEmptyList()
    {
        var (sprintId, token) = await SetupVotingSprintWithParticipantAsync("Ana");

        var res = await GetMyVotes(token, sprintId);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyVotes_NoGuestToken_Returns401()
    {
        var res = await _anonClient.GetAsync("/api/sprints/1/votes/mine");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyVotes_WrongSprint_Returns401()
    {
        var (_, token) = await SetupVotingSprintWithParticipantAsync("Ana");

        var res = await GetMyVotes(token, 9999);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- ListCards with VoteCount ---

    [Fact]
    public async Task ListCards_IncludesVoteCount()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");
        var (_, secondToken) = await JoinSprintAsync(sprintId, "Ben");
        await PostVote(token, sprintId, groupId);
        await PostVote(secondToken, sprintId, groupId);

        var res = await GetCards(token, sprintId);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        var card = body!.First(c => c.GetProperty("groupId").GetInt32() == groupId);
        card.GetProperty("voteCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ListCards_NoVotes_VoteCountIsZero()
    {
        var (sprintId, token, groupId) = await SetupSprintWithNoteGroupAsync("Ana");

        var res = await GetCards(token, sprintId);
        var body = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        var card = body!.First(c => c.GetProperty("groupId").GetInt32() == groupId);
        card.GetProperty("voteCount").GetInt32().Should().Be(0);
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

    private async Task AdvancePhaseAsync(int sprintId)
    {
        await _client.PostAsJsonAsync($"/api/teams/{_teamId}/sprints/{sprintId}/advance-phase", new { });
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

    private async Task<(int sprintId, string token)> SetupVotingSprintWithParticipantAsync(string name)
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync(name);
        await AdvancePhaseAsync(sprintId);
        await AdvancePhaseAsync(sprintId);
        return (sprintId, token);
    }

    private async Task<int> CreateNoteCardAsync(string token, int sprintId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/sprints/{sprintId}/cards")
        {
            Content = JsonContent.Create(new { text = "Test note", columnType = "Column1", cardType = "Note" }),
        };
        req.Headers.Add("X-Guest-Token", token);
        var res = await _client.SendAsync(req);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private async Task<int> GetGroupIdForCardAsync(string token, int sprintId, int cardId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/cards");
        req.Headers.Add("X-Guest-Token", token);
        var res = await _client.SendAsync(req);
        var cards = await res.Content.ReadFromJsonAsync<JsonElement[]>();
        var card = cards!.First(c => c.GetProperty("id").GetInt32() == cardId);
        return card.GetProperty("groupId").GetInt32();
    }

    private async Task<(int sprintId, string token, int groupId)> SetupSprintWithNoteGroupAsync(string name)
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync(name);
        var cardId = await CreateNoteCardAsync(token, sprintId);
        // Advance Writing -> Grouping (auto-creates solo groups) -> Voting
        await AdvancePhaseAsync(sprintId);
        await AdvancePhaseAsync(sprintId);
        var groupId = await GetGroupIdForCardAsync(token, sprintId, cardId);
        return (sprintId, token, groupId);
    }

    private async Task<(int sprintId, string token, int[] groupIds)> SetupSprintWithNoteGroupsAsync(string name, int count)
    {
        var (sprintId, token) = await SetupActiveSprintWithParticipantAsync(name);
        var cardIds = new int[count];
        for (var i = 0; i < count; i++)
            cardIds[i] = await CreateNoteCardAsync(token, sprintId);
        await AdvancePhaseAsync(sprintId);
        await AdvancePhaseAsync(sprintId);
        var groupIds = new int[count];
        for (var i = 0; i < count; i++)
            groupIds[i] = await GetGroupIdForCardAsync(token, sprintId, cardIds[i]);
        return (sprintId, token, groupIds);
    }

    private Task<HttpResponseMessage> PostVote(string token, int sprintId, int groupId)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/sprints/{sprintId}/card-groups/{groupId}/votes")
        {
            Content = JsonContent.Create(new { }),
        };
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> DeleteVote(string token, int sprintId, int groupId)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/sprints/{sprintId}/card-groups/{groupId}/votes");
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> GetMyVotes(string token, int sprintId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/votes/mine");
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }

    private Task<HttpResponseMessage> GetCards(string token, int sprintId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/sprints/{sprintId}/cards");
        req.Headers.Add("X-Guest-Token", token);
        return _client.SendAsync(req);
    }
}

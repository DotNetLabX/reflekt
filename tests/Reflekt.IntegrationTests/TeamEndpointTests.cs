using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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
        _authFactory = ReflektApiFactory.CreateSignedIn(_testDbPath, Policies.SignedInUser);
        _anonFactory = ReflektApiFactory.CreateAnonymous(_anonDbPath);

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
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("statusCode").GetInt32().Should().Be(400);
        body.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        var errors = body.GetProperty("errors");
        errors[0].GetProperty("field").GetString().Should().Be("name");
        errors[0].GetProperty("message").GetString().Should().NotBeNullOrEmpty();
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
        await ReflektApiFactory.DeleteDatabaseFilesAsync(_testDbPath, _anonDbPath);
    }
}

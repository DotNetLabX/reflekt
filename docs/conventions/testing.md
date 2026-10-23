# Testing Conventions

How this repo tests today. For the TDD workflow (red-green-refactor loop), see the `tdd` skill.

## Backend (.NET)

### Projects and layout

```
tests/
  Reflekt.UnitTests/               # aggregate behaviour, no host, no database
    {Area}/{ClassUnderTest}Tests.cs  # one folder per domain area: Cards/, Participants/, Sprints/,
                                     # Teams/, Votes/, EvidenceLinks/ (source detector), BuildingBlocksTests/
  Reflekt.IntegrationTests/        # endpoints through WebApplicationFactory<Program>
    {Area}EndpointTests.cs         # flat at the project root, one class per endpoint area
    AuthSmokeTests.cs, FoundationSmokeTests.cs
    TestParallelization.cs         # [assembly: CollectionBehavior(DisableTestParallelization = true)]
    Helpers/                       # the shared helpers below
```

- Unit tests reference `Reflekt.API`, `Reflekt.Domain` and `Blocks.Domain`; integration tests reference `Reflekt.API`.
- Integration tests run one at a time: FastEndpoints keeps process-wide static state, so parallel hosts interfere.

### Naming

- Test class: `{ClassUnderTest}Tests` (`SprintTests`, `CardTests`) or `{Area}EndpointTests` (`CardEndpointTests`).
- Test method: `{Action}_{Condition}_{Outcome}` — `Create_WithEmptyName_Throws`, `Start_FromDraft_TransitionsToActive`, `CreateCard_ClosedSprint_Returns409`.
- Accepted short form when there is no condition worth naming: `{Action}_{Outcome}` — `Application_Starts`, `Login_RedirectsToGoogle`, `Create_RaisesTeamCreatedEvent`.
- Many small focused tests per behaviour; each test asserts one outcome.

### Unit tests

Call the aggregate's factory and behaviour methods directly; there is no shared test-data file and no builder.

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
public void Create_WithEmptyName_Throws(string name)
{
    var act = () => Team.Create(name, 42);
    act.Should().Throw<ArgumentException>();
}
```

### Integration tests and their helpers

Each test class creates its hosts in `InitializeAsync` on its own temporary SQLite file and deletes the file in `DisposeAsync`. The helpers live in `tests/Reflekt.IntegrationTests/Helpers/`:

| Helper | What it gives |
|---|---|
| `ReflektApiFactory` | `CreateAnonymous(dbPath)` and `CreateSignedIn(dbPath, policies…)` — a `WebApplicationFactory<Program>` on the given database file, seeding off; the signed-in host adds the `Test` scheme to the named policies. `DeleteDatabaseFilesAsync(paths…)` clears the SQLite pools and deletes the files. |
| `TestAuthHandler` | the `Test` scheme: a signed-in user `TestUserId = 1`, `test@example.com` |
| `TestDataSeeder` | extensions on the factory: `SeedTestUserAsync`, `SeedDiscussedGroupsAsync`, `CreateScrumMasterCookie` |
| `TestGoogleCredentials` | `UsePlaceholderGoogleCredentials()` — placeholder `Auth:Google:*` values, so the host passes its start-up options check (used by `ReflektApiFactory`) |

```csharp
public class TeamEndpointTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-reflekt-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _authFactory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _authFactory = ReflektApiFactory.CreateSignedIn(_testDbPath, Policies.SignedInUser);
        _client = _authFactory.CreateClient();
        await _authFactory.SeedTestUserAsync();
    }

    [Fact]
    public async Task CreateTeam_Authenticated_ReturnsCreatedTeam()
    {
        var response = await _client.PostAsJsonAsync("/api/teams", new { name = "My Team" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public async Task DisposeAsync()
    {
        await _authFactory.DisposeAsync();
        await ReflektApiFactory.DeleteDatabaseFilesAsync(_testDbPath);
    }
}
```

Guest calls send the participant's token in the `X-Guest-Token` header, as the SPA does.

### Assertions

FluentAssertions exclusively. No `Assert.Equal`.

### Mocking

No mocking library is used. Domain logic is tested by calling the aggregate; endpoints, authorization and persistence are tested for real through `WebApplicationFactory`, `TestAuthHandler` and a temporary SQLite file per test class.

### Package versions

- **FluentAssertions is kept at 7.2.0 on purpose**; later majors are not adopted. Its 8.0.0 release notes say: "Update ownership and license to the Xceed Community License".
- xUnit 2.x (`xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4), `Microsoft.NET.Test.Sdk` 17.14.1 and `coverlet.collector` 6.0.4 are kept as they are (F37).

### Speed trait and the verify roles

`.claude/verify.json`'s `unit` role runs `dotnet test tests/Reflekt.UnitTests/… --filter "Speed!=Slow"`. No test carries a `Speed` trait today, so the filter runs every unit test. A unit test that is slow (over the 2000 ms threshold in `verify.json` § `slow`) may be marked `[Trait("Speed", "Slow")]`; it then runs only in the full suite. The `integration` role runs the integration project; `full` runs the whole solution.

## Frontend (Vue/TypeScript)

Frontend testing is not set up: there is no test runner, no test script in `web/reflekt-web/package.json` and no test files. Frontend changes are checked by `npm run build` (type check) and `npm --prefix web/reflekt-web run lint`.

## Coverage Expectations

Not every change needs new tests. Prioritize:
- **Unit tests** for aggregate behaviour: factory guards, the sprint state machine (Draft → Active → Closed) and the phase machine (Writing → … → Reporting), and the domain events each method raises.
- **Integration tests** through `WebApplicationFactory` for endpoints: status codes and bodies, authorization (each policy, `SprintAccess` refusals), phase-action gating (an action outside its phase returns 409), and the error body.
- Edge cases flagged in plan steps.

Skip tests for:
- Pure wiring (DI registration, EF configuration, endpoint routing on its own)
- UI layout and styling

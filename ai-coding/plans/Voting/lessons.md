# Voting — Lessons

## Developer Lessons

### POST endpoints with route-only params need an empty JSON body in tests
FastEndpoints returns 415 (Unsupported Media Type) when a POST request arrives without `Content-Type: application/json`, even if the request model is bound entirely from route parameters. Test helpers for POST endpoints must always set a JSON body:
```csharp
Content = JsonContent.Create(new { })
```
This matches the pattern in `CardEndpointTests` where POST helpers always send a payload.

### Auth-before-lookup means some 404 paths are unreachable
When an endpoint checks `sprintId` claim vs route `sprintId` first (returning 401 on mismatch), and the sprint lookup comes after — you can never get a 404 for a nonexistent sprint via guest token, because the claim mismatch fires first. Plan test cases that assume 404 for nonexistent IDs need to be re-evaluated: if auth fires first, the correct expected status is 401.

### `VoteDistributionEntry` defined in two namespaces — no ambiguity issue
The repository defines its own `VoteDistributionEntry` return type, and the API contract has one too. C# resolves the name in the endpoint via parent-namespace precedence (API contract wins over `using` import from Persistence). No explicit alias needed.

### Vote store `votesPerParticipant` uses default 3
The actual sprint `VotesPerParticipant` isn't fetched in the frontend for v1. The vote store defaults to 3. If a sprint has a different budget, the remaining budget display will be wrong. A future improvement: include `votesPerParticipant` in the join response or a sprint details endpoint.

## Skill Gaps

### Missing skill: `create-feature` for DELETE endpoints with no body
The existing `create-feature` skill covers POST/PUT mutations well. For DELETE endpoints that have route-only params (like `RetractVote`), the skill doesn't note that no validator is needed and no body is expected. Referenced `DeleteCardEndpoint.cs` instead.

### Missing skill: integration test helper pattern for no-body POST
No skill documents the `Content = JsonContent.Create(new { })` pattern for POST endpoints with no request body. Learned via test failure.

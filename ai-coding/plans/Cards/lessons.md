# Cards — Lessons

## Developer Lessons

- **`dotnet build -q` is broken on this machine.** The `-q` (quiet) flag produces "Question build FAILED" output and a misleading error even when the build actually succeeds. Always use full output (`dotnet build Reflekt.slnx`) for accurate results.

- **Integration test auth factories need an anonymous client for 401 tests.** Any test verifying "no auth → 401" must use a `WebApplicationFactory` without `TestAuthHandler`. `_client` with TestAuthHandler always authenticates as SM, so unauthenticated test cases silently pass as SM. Follow the `_anonFactory`/`_anonClient` pattern from `ParticipantEndpointTests`.

- **Claim check before DB lookup changes expected status codes.** `CreateCardEndpoint` checks the `sprintId` claim against the route before loading the sprint. A request to a nonexistent sprint with a mismatched token returns 401 (claim mismatch), not 404. Write integration tests that reflect the actual handler flow order, not what feels semantically correct.

- **Vue components referenced in views must be created explicitly.** `RetroRoomView.vue` needed `CardItem.vue` but the plan's file summary didn't list it. When a view references a new component, create it — don't skip it because it wasn't in the plan's file list.

- **Parent namespace types are accessible without `using` in C#.** Types in `Reflekt.API.Features.Cards` are visible from `Reflekt.API.Features.Cards.ListCards` without an explicit `using`. This is standard C# namespace lookup behavior — no need to add redundant using statements for parent namespaces.

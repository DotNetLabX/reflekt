# Sprints — Lessons

## Developer Lessons

### FastEndpoints POST with route-only request model requires Content-Type
Even when a POST endpoint's request model binds entirely from route params (no body properties), FastEndpoints returns 415 Unsupported Media Type if the `Content-Type` header is absent. In integration tests, use `PostAsJsonAsync(url, new { })` instead of `PostAsync(url, null)` for all POST endpoints, regardless of whether they have a body.

### MSBuild cache lock during parallel builds (Windows, .NET 10)
On Windows with VS Code / language server running, MSBuild intermittently fails with `MSB3492: Could not read existing file *.cache`. This is a file-locking race condition — it is transient and not a compilation error. Diagnosis: filter build output for `CS[0-9]+` errors rather than the string "error". The build succeeds even when MSB3492 appears in output. Running `dotnet build <specific-project>` directly (not via the solution) reliably succeeds.

### Partial class `private set` accessible across files
In a partial class split (properties file + behaviors file), `private set` properties defined in one partial are fully accessible from other partials in the same class — including static factory methods calling instance setters on newly constructed objects. No workarounds needed.

### Enum-to-string in API responses: prefer explicit `.ToString()` over JSON converter
When response DTOs already use `string` for state/phase fields (as planned in `SprintResponse`), calling `.ToString()` in the mapping helper is simpler than configuring a global `JsonStringEnumConverter`. The converter approach is needed only if the response type uses the enum directly.

## Architect Lessons

### Plans must specify Content-Type + empty body for bodyless POST actions in frontend stores
The plan described frontend store actions for `startSprint`, `advancePhase`, `closeSprint` as simple POST calls but didn't specify request options. The Developer followed the pattern from `createSprint` (which has a body) for that action, but used bare `fetch` for the bodyless ones — causing 415 errors. **Fix for future plans:** When a plan step describes frontend actions that call POST endpoints without a request body, explicitly state: "include `headers: { 'Content-Type': 'application/json' }, body: '{}'` — FastEndpoints rejects POST without Content-Type even when the endpoint binds only from route params."

### Integration tests catch frontend bugs early
The Developer's integration tests discovered the Content-Type/415 issue (using `PostAsJsonAsync` as a workaround) before the frontend was written — but that knowledge didn't propagate to the frontend store implementation. When a test reveals a framework quirk, the plan should be updated with a note on the affected frontend step, not just the test step.

## Developer Lessons

### FastEndpoints auto-sends 204 for `EndpointWithoutRequest` when no `Send*` is called
If `HandleAsync` returns without calling any `SendAsync`/`SendOkAsync`/etc., FastEndpoints 5.34.0 sends 204 NoContent. For endpoints that write to the response outside FastEndpoints (e.g. `ChallengeAsync`, `SignOutAsync` followed by a redirect), call `await HttpContext.Response.StartAsync(ct)` after the non-FastEndpoints response write to flush headers and set `HasStarted = true` — preventing the auto-204.

### Cookie auth redirects to `/Account/Login` by default; SPAs need 401 instead
Without `OnRedirectToLogin`, unauthenticated requests to cookie-guarded endpoints get a 302 to `/Account/Login`. Test clients follow this redirect and land on `index.html` (200). Fix in `AuthenticationConfiguration.cs` — add `Events.OnRedirectToLogin` and `Events.OnRedirectToAccessDenied` handlers that set the status code directly. This is both a test fix and required production behavior for any SPA.

### `Policies` class name conflicts with FastEndpoints `Policies(...)` method
Inside `Configure()`, `Policies(Policies.ScrumMaster)` fails with CS0119 because `Policies` resolves to the class, not the method. Use a type alias: `using AuthPolicies = Reflekt.API.Auth.Policies;` and call `Policies(AuthPolicies.ScrumMaster)`.

### `internal` helpers in `Reflekt.API` are not accessible from `Reflekt.UnitTests` by default
The unit test project doesn't reference `Reflekt.API`. Options: (a) make the helper `public` and add the `ProjectReference`, or (b) keep `internal` + add `InternalsVisibleTo`. For pure utility helpers (no framework dependencies), `public` + reference is simpler.

---

## Architect Lessons

### Specify `EndpointWithoutRequest<TResponse>` when the endpoint has a typed response
Plan §3.3 spec'd `EndpointWithoutRequest` — the Developer silently corrected to the generic form. Always write the generic parameter in the plan when a response record exists; it prevents a silent deviation and documents intent.

### When planning auth / middleware features, investigate framework defaults alongside the happy-path code
This plan left three discoveries to the Developer: FastEndpoints' auto-204 swallowing `ChallengeAsync`, cookie auth's 302 → `/Account/Login` default, and the `Policies` class/method name collision. Each was a real friction point. For the next plan touching middleware, run a short "what does this framework do by default when I don't call `Send*`?" pass before writing the slice spec.

### Security-critical manual tests must be flagged, not buried in a verification list
Plan §7.4 (allowlist rejection) is the primary security boundary. It sat as one of six acceptance criteria. It ended up "NOT TESTED by Developer" — understandable given OAuth-setup friction, but the plan should have said at the top: *"This feature is not done until §7.4 passes in a browser. No integration test covers the callback rejection path."* Future auth/security plans: put the manual-only tests in a dedicated "Cannot ship without these" section, separate from the general verification list.

### Test-helper access (`public` + ProjectReference vs `InternalsVisibleTo`) is a repo convention, not a per-feature choice
Auth was the first feature with a test-only helper. The path-of-least-resistance (`public` + reference) got picked by the Developer because the plan said `internal` without a ProjectReference. Every future feature that adds a helper inherits this precedent. Next plan (Teams) is the right moment to pick the repo-wide convention; leaving it to drift per-feature means refactoring 3–5 classes at feature #5.

### The Step 1 done.md gate is worth enforcing, even when the Developer says "it's done"
First pass of done.md was missing setup docs, manual-flow confirmation, and the allowlist-rejection test status. Sending back cost 10 minutes and added a reusable "Local setup" section + honest NOT-TESTED flags. Keep the gate strict — a silent done.md hides real verification gaps.

### Cross-check the plan against framework defaults, not just the spec
I caught one thing via spec cross-check: scope-list preservation (spec §3.1). I missed three things that would have shown up via a *framework* cross-check: cookie redirect defaults, FastEndpoints auto-204, `Policies` name collision. Add to the cross-check routine: "for every framework call in the plan, read its documented default behavior; if it conflicts with plan intent, spell out the override."

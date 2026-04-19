# Foundation Review (second cycle — after Simplify pass)

## Step 1: Done check — one gap

`done.md` adds a `### Simplify pass` section with 10 items plus the prior `### Post-session fixes` block. All simplify items are mapped to files. One problem:

- **Prior review finding #1 (EF Design duplicate) is still not addressed.** `Microsoft.EntityFrameworkCore.Design` still appears on `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj` (lines 11–14) with no mention in `done.md` of it being removed or of a decision to keep it. The prior review tagged it "not a blocker," but "not a blocker" means "ship Foundation with it" — not "skip it on the next review pass." We want one source of truth per `ai-coding/conventions` and per the Architect lesson in `lessons.md` ("EF Design belongs on the startup project, not the migrated project").

Step 1 gate result: **failed on one line**. See "What Developer should do" at the bottom.

## Step 2: Code review of the Simplify pass

Because I already had the relevant files open, I spot-checked the simplify changes against the plan contract. All of them are sound — none violate plan invariants. Details below.

### `AggregateRoot : Entity` (deduplicating `Id`)
- `AggregateRoot.cs`: `public abstract class AggregateRoot : Entity` with `List<IEvent> _domainEvents` + `IReadOnlyList<IEvent> DomainEvents` + `AddDomainEvent` + public `ClearDomainEvents`.
- `Entity.cs`: still owns `int Id`, equality by type + Id, `==`/`!=`.
- Plan §6 currently describes them as two independent abstracts. Composition is strictly better — removes a duplicate `Id` definition. **Plan should be updated** to reflect the new hierarchy (AggregateRoot extends Entity).

### `GuestTokenDefaults` + `Policies` constants
- New `GuestTokenDefaults.AuthenticationScheme = "GuestToken"` replaces the magic string in `AuthenticationConfiguration`, `AuthorizationConfiguration`, and `SprintHub[Authorize(...)]`.
- New `Policies.{ScrumMaster,Participant,OwnerOrScrumMaster}` replaces policy-name literals in `AuthorizationConfiguration` and will replace them in endpoints.
- Plan §9 currently uses string literals; **should be updated** to reference these constants for future feature authors.

### `OpenGraphConfiguration`
- `Channel.CreateBounded<FetchJob>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait })` — plan contract intact. The done.md phrase "dropped `Channel<FetchJob>` full-channel singleton" is misleading; the bounded channel is still registered, only `channel.Reader` and `channel.Writer` are.
- `MaxResponseContentBufferSize = 2 * 1024 * 1024` (2 MB) — new hardening. Not in the plan but aligns with the SSRF-guarded spirit of the feature. **Plan should be updated** to mention this as a response-size ceiling.

### `OpenGraphFetcher`
- Bare `catch {}` → `catch (Exception ex) when (ex is not OperationCanceledException)` — correct: preserves cancellation propagation.
- `EnsureSuccessStatusCode()` → `if (!response.IsSuccessStatusCode) return FetchResult.Failed;` — avoids the try/catch-on-exception anti-pattern.
- `Configuration.Default` cached as `static readonly AngleSharp.IConfiguration _angleSharpConfig` — standard AngleSharp reuse pattern.
- `Dns.GetHostAddressesAsync(host, ct)` — CancellationToken threaded through the DNS lookup.
- Plan §12 doesn't describe these in this much detail; they are all "how" rather than "what" — no plan update needed.

### `FoundationSmokeTests`
- Test DB path now unique per run (`Path.GetTempPath() + Guid`). Eliminates the Windows file-lock race where back-to-back test runs stomp on each other's `.db`. Still calls `ClearAllPools()` and deletes the temp file in `DisposeAsync`. Good.
- Plan §14 can be **updated** with "test DB path should be unique per test run" since this pattern will recur.

### Prior review finding status

| # | Finding | Status |
|---|---------|--------|
| 1 | EF Design duplicate on Reflekt.Persistence.csproj | **NOT applied** — blocker for close-out |
| 2 | `guest.ts` types `sprintId: string` instead of `number` | Deferred to Participants feature — still correct |
| 3 | `counter.ts` Pinia scaffold residue | Still present — cleanup on first store change |
| 4 | `Entity.GetHashCode` uses only `Id` not type | Still unchanged — fine for Foundation |

## What Developer should do

One line of cleanup:

1. Remove the `<PackageReference Include="Microsoft.EntityFrameworkCore.Design">` block from `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj` (lines 11–14). EF Design lives on the startup project (`Reflekt.API.csproj`) only.
2. Update `done.md` under `### Simplify pass` or a new `### Review cycle 2 fixes` block to note the removal.

After that, Foundation is done.

## What Architect will do

Before the next feature (Teams), update the plan to ratify the simplify-pass changes so the plan reflects the real contract:

- §6: describe `AggregateRoot : Entity` composition
- §9: reference `GuestTokenDefaults.AuthenticationScheme` and `Policies.*` instead of string literals
- §12: add `MaxResponseContentBufferSize` ceiling
- §14: note unique-per-run test DB path as the standard pattern

## Verdict

**One-line fix away from done.** Safe to start **Teams** immediately after finding #1 is removed (or ratified in the plan if there's a reason to keep it, but none has been given).

---

## Cycle 3 close-out

- `Reflekt.Persistence.csproj` no longer references `Microsoft.EntityFrameworkCore.Design` — only `Microsoft.EntityFrameworkCore.Sqlite` remains. `Reflekt.API.csproj` remains the single source for EF Design (startup project).
- `done.md` `### Review cycle 2 fix` block documents the removal.

**Foundation is closed.** Ready to plan **Teams**. Before writing the Teams plan, I'll ratify the simplify-pass changes in `plan.md` (§6 hierarchy, §9 auth constants, §12 response buffer, §14 unique test DB path) so the contract matches the code.

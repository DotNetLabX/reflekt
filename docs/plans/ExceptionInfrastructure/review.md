# ExceptionInfrastructure — Review

## Reviewed By
`reviewer` (Sonnet 4.6 agent)

## Verdict: APPROVE

## Pre-commitment Predictions

| Prediction | Actual |
|------------|--------|
| Namespace rename may have missed files | Only a comment reference in `OpenGraphFetcher.cs` remains — intentional (a code comment, not a using). Clean. |
| RepositoryExtensions type constraints wrong | Constraints are correct: `Entity<int>` used, matching the actual base class. |
| Middleware wiring order incorrect | Middleware is registered first in `UseReflektApi`, before `UseRouting`, `UseAuthentication`, `UseAuthorization`. Correct placement. |
| Frontend error shape not updated | Frontend already uses `throw { status, body }` pattern and generic `onError` string messages — not parsing specific ProblemDetails fields. No shape dependency broken. |
| `Guard.ThrowIfFalse` as extension method unusual | Valid C# — extension methods on `bool` defined in a static class are fine. Not an issue. |

## Findings

### [MEDIUM] HandleExceptionAsync does not guard against response already started

**File:** `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs:46-61` and `:63-74`

**Issue:** `HandleExceptionAsync` and `HandleValidationExceptionAsync` call `WriteResponseAsync` unconditionally. If `next(context)` has already begun streaming a response before throwing (e.g., a chunked response), writing to `context.Response` will throw an `InvalidOperationException: Response has already started`. The `OperationCanceledException` branch (line 37) correctly guards with `!context.Response.HasStarted`, but the general exception path does not.

**Fix:** Add `if (context.Response.HasStarted) { logger.LogError(exception, "Exception after response started. TraceId={TraceId}", context.TraceIdentifier); return; }` at the top of `HandleExceptionAsync`. Same guard in `HandleValidationExceptionAsync`.

**Self-audit confidence:** HIGH. This is a known ASP.NET Core middleware pitfall, not a style preference. In the current Reflekt codebase this is low-risk because no endpoints stream responses, but it is a correctness issue that will surface if streaming is added.

---

### [LOW] Step 7 (skill updates) not completed — permission denied

**File:** `.claude/skills/error-handling/SKILL.md`

**Issue:** Implementation.md documents that the error-handling skill still references `Blocks.AspNetCore/Middlewares/` as the middleware path, which is incorrect for the current implementation (middleware lives in `{Svc}.API/Middleware/`). The developer could not write to `.claude/skills/` files. This is a process gap, not a code defect — no runtime impact.

**Fix:** Grant write permission for `.claude/skills/` to the developer agent and complete Step 7 in a follow-up, or update the skill manually.

---

### [LOW] FindByIdAsync and GetByIdAsync are identical implementations

**File:** `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs:19-26`

**Issue:** `GetByIdAsync` (line 19) and `FindByIdAsync` (line 25) both call `Db.Set<TEntity>().FindAsync([id], ct)` with identical bodies. The plan noted this alias was intentional for clarity, but it creates dead code weight and a subtle surprise for future callers who expect them to differ.

**Fix:** Consider removing `FindByIdAsync` and updating `RepositoryExtensions.FindByIdOrThrowAsync` to call `GetByIdAsync` directly, keeping one method. Or document the alias with a comment. Either is acceptable.

---

### [LOW] SingleOrThrow extension lives in Blocks.Exceptions project under Blocks.Linq namespace

**File:** `src/BuildingBlocks/Blocks.Exceptions/Extensions.cs:3`

**Issue:** The file is in `Blocks.Exceptions` project but declares `namespace Blocks.Linq`. The comment explains this matches the reference project convention. The asymmetry between the assembly and the namespace will require callers to add `using Blocks.Linq` when they already `using Blocks.Exceptions`, which is non-obvious. This is a documented convention, not a bug.

**Fix:** None required. Existing comment documents the intent. If the discoverability is a future concern, move the file to a `Blocks.Linq` project when one exists.

## Positive Observations

- **Build is clean:** 0 warnings, 0 errors, all 9 projects compile. The namespace bulk-replace across 24+ domain files was executed correctly — no stale references in source.
- **Switch expression ordering in middleware is correct:** `ValidationException` is caught by its own dedicated handler. The general `catch (Exception ex)` correctly covers `BadRequestException`, `NotFoundException`, `DomainException`, and `ArgumentException` via `MapStatusCode`. Class hierarchy is clean (none of the `Blocks.Exceptions` types inherit from `ArgumentException`), so the switch ordering creates no shadowing.
- **Middleware placement is correct:** Registered before `UseRouting`, `UseAuthentication`, `UseAuthorization` — it will catch exceptions from all downstream middleware and endpoints.
- **AddProblemDetails and dev exception page fully removed:** Grep confirms no `UseExceptionHandler` or `AddProblemDetails` in any source file. Clean replacement.
- **Frontend is not broken:** The Vue stores throw `{ status, body }` objects and the composable catches all errors with generic `onError` string messages — no coupling to ProblemDetails field names. The new middleware JSON shape (`StatusCode`, `Message`, `TraceId`, `Details`) does not break existing frontend error handling.
- **RepositoryExtensions type constraints are correct:** Uses `Entity<int>` (the actual base class in this codebase), not a fictional `IEntity<int>` interface.
- **DomainException primary constructor pattern** is idiomatic C# 12 and matches the plan specification exactly.
- **Directory.Build.props covers new packages:** `Blocks.Exceptions` and `Blocks.Core` csproj files have no `<TargetFramework>` by design — they inherit from the root `Directory.Build.props`, which sets `net10.0`, `Nullable enable`, `ImplicitUsings enable`. Build confirms this works.
- **Solution file is correct:** `Reflekt.slnx` references all four `Blocks.*` packages. No old `BuildingBlocks.*` paths remain in the solution file.

## Gaps

- **No unit tests for new exception types or Guard utilities.** The plan did not require them, so this is not a plan deviation. However `Guard.NotFound`, `OrThrowNotFound`, and `SingleOrThrow` are pure functions that would be trivial to test and would provide a regression net.
- **RepositoryExtensions are not yet used anywhere** in the Reflekt API (no endpoints call `FindByIdOrThrowAsync` etc.). They are infrastructure for future use. This is expected per the plan scope.
- **Frontend 499 handling:** Client disconnect returns status 499 with no body. No frontend code was verified to handle a 499 specifically — but since it only occurs on cancelled requests, the browser-side fetch will also be aborted, so the response is never read. Not a gap.

## Open Questions

- None. All predictions confirmed or resolved.

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | PASS | `dotnet build --no-incremental` | Build succeeded. 0 Warning(s). 0 Error(s). 9 projects compiled. |
| Remaining BuildingBlocks in .cs source | PASS | grep for `BuildingBlocks` in `src/**/*.cs` | Only `OpenGraphFetcher.cs:2` — a code comment (`// Extract to BuildingBlocks.Api if a second service...`), not a using or namespace declaration. |
| Remaining BuildingBlocks in .csproj source | PASS | grep for `BuildingBlocks` in `src/**/*.csproj` | Only in `ProjectReference` paths containing the parent folder `BuildingBlocks\` (the container folder name is unchanged by design — only project subfolders were renamed). |
| Remaining BuildingBlocks in tests | PASS | grep for `BuildingBlocks` in `tests/**/*.cs` | Only `namespace Reflekt.UnitTests.BuildingBlocksTests` — a test namespace name, not a using or project reference. No impact. |
| Solution file | PASS | Read `Reflekt.slnx` | All 4 packages listed as `Blocks.*`. No old paths. |
| Middleware registration | PASS | Read `DependencyInjection.cs` | `UseMiddleware<GlobalExceptionMiddleware>()` is first call in `UseReflektApi`, before routing/auth. |
| Try/catch removal | PASS | Read 3 endpoint files | `CreateCardEndpoint`, `EditCardEndpoint`, `UpdateGroupTitleEndpoint` — no try/catch blocks present. |
| Directory.Build.props | PASS | Read file | `net10.0`, Nullable, ImplicitUsings set. New csproj files correctly inherit these. |

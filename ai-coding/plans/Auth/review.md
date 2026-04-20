# Auth Review — Step 2 (Code Review)

**Step 1 (done.md check):** PASS after Developer added Local setup + verification section.

**Step 2 overall:** Code implementation matches plan. No code-level fixes required as blockers. Two undisclosed minor deviations, one missing test case, and four "plan was wrong" deviations (all disclosed, all justified). Manual verification §7.3 / §7.4 remains pending — feature not fully "done" until those pass.

---

## 1. Plan adherence — per section

| Plan section | Status | Notes |
|---|---|---|
| §3.1 LoginEndpoint | ✅ + disclosed deviation #3 | `StartAsync(ct)` added; plan was wrong |
| §3.2 LogoutEndpoint | ✅ + disclosed deviation #4 | `AuthPolicies` alias; plan was imprecise |
| §3.3 MeEndpoint | ✅ + undisclosed deviation | See §2 below |
| §3.4 AllowlistChecker | ✅ + disclosed deviation #1 | `public` not `internal`; see §4 below |
| §3.5 AuthenticationConfiguration | ✅ + disclosed deviation #2 | Cookie `OnRedirectTo*` overrides; plan was wrong |
| §3.5 (scope list guard) | ✅ | Scope list preserved: `openid` + `email` + `profile`. Cross-check worked. |
| §3.6 AuthOptions unchanged | ✅ | Confirmed unchanged |
| §4.1 auth.ts | ✅ | Exact match |
| §4.2 AppHeader | ✅ | Exact match |
| §4.3 WelcomeView | ✅ | Exact match |
| §4.4 NotInvitedView | ✅ | Exact match |
| §4.5 router | ✅ | Exact match |
| §4.6 App.vue | ✅ | Exact match |
| §5.1 AllowlistCheckerTests | ⚠️ | Missing whitespace case; see §3 below |
| §5.2 AuthSmokeTests | ✅ | All 3 tests match plan |
| §7.5 Program.cs untouched | ✅ | Still 8 non-blank lines + `public partial class Program { }` (unchanged from Foundation) |
| §7.6 No DbSet / migration | ✅ | Persistence layer untouched |

---

## 2. Undisclosed deviations

### 2.1 `MeEndpoint` uses `EndpointWithoutRequest<MeResponse>` (plan said `EndpointWithoutRequest`)

Plan §3.3 spec'd `public sealed class MeEndpoint : EndpointWithoutRequest`. Code uses the generic form `EndpointWithoutRequest<MeResponse>`.

**Verdict:** Plan was imprecise. Generic form is the idiomatic FastEndpoints pattern for a typed GET response — it gives you `SendOkAsync(MeResponse, ct)` with the right overload resolution. **No code fix.** Captured in `lessons.md` for future plans (use `EndpointWithoutRequest<TResponse>` when the response is typed).

### 2.2 `Reflekt.UnitTests.csproj` pulls `Reflekt.API` + `Reflekt.Domain` + `BuildingBlocks.Domain`

Done.md mentions the new `Reflekt.API` ProjectReference but the csproj also has `Reflekt.Domain` and `BuildingBlocks.Domain` references (probably Foundation-era). Not a problem, just noting the unit-test project's reference graph is getting wide.

**Verdict:** Not a blocker. But see §4 on the convention decision for helper-class access.

---

## 3. Minor code issue — missing test case

`AllowlistCheckerTests` has 5 tests (matches plan count) but plan §5.1 said: *"null or whitespace email → false"*. Code tests null only. `AllowlistChecker` uses `IsNullOrWhiteSpace`, so a whitespace email (`"   "`) is also rejected — but there's no test proving it.

**Fix:** rename `NullEmail_ReturnsFalse` → two tests: `NullEmail_ReturnsFalse` and `WhitespaceEmail_ReturnsFalse` (input `"   "`). 2 minutes. Not blocking — the implementation is correct, just undertested.

---

## 4. Correction required — revert `AllowlistChecker` to `internal`; adopt `InternalsVisibleTo`

**Decision (by user):** repo convention is `internal` helpers + `InternalsVisibleTo` for test access. Auth is the first feature to hit this, so we fix it here rather than let `public` propagate.

**Developer actions:**

1. Change `src/Services/Reflekt/Reflekt.API/Auth/AllowlistChecker.cs` from `public static class` → `internal static class`.
2. In `src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj`, add:
   ```xml
   <ItemGroup>
     <InternalsVisibleTo Include="Reflekt.UnitTests" />
   </ItemGroup>
   ```
3. Keep the `ProjectReference` in `tests/Reflekt.UnitTests/Reflekt.UnitTests.csproj` — it's still needed for compile.
4. Update `done.md` Deviations — reclassify item #1 as "reverted per review; `InternalsVisibleTo` adopted as repo convention for test-helper access."

**Why:** `public` means any project in the solution sees the helper; `InternalsVisibleTo` keeps it invisible to app code but whitelists the test project. Same test coverage, smaller public API surface. See `lessons.md` for the architect rationale.

---

## 5. Manual verification gap — risk accepted (no Developer action)

done.md §7.3 and §7.4 remain **NOT TESTED**. Integration tests cover entry (login redirects to Google) and unauthenticated paths (401s); the callback rejection (primary security boundary) has zero automated coverage.

**User decision:** risk accepted for v1. We proceed to Teams without running the browser flow. If the allowlist rejection turns out to break in practice, that's a fix-Auth task taken at the point of failure, not a pre-Teams blocker.

**No Developer action.** Leave done.md §7.3 / §7.4 as NOT TESTED.

---

## 6. What I'm updating (not in this file)

- `ai-coding/plans/Auth/lessons.md` — patterns future plans need (FastEndpoints + ChallengeAsync, `AuthPolicies` alias, cookie `OnRedirectTo*` overrides, `EndpointWithoutRequest<T>` form). See that file.

## 7. Verdict & Developer action list

**Code review: PASS with corrections required before Auth is closed out.**

**Actions required (§3 + §4):**
1. Add `WhitespaceEmail_ReturnsFalse` test to `AllowlistCheckerTests.cs` (input `"   "`, expect `false`).
2. Revert `AllowlistChecker` to `internal` + add `<InternalsVisibleTo Include="Reflekt.UnitTests" />` to `Reflekt.API.csproj` (see §4 for exact steps).
3. Update `done.md` deviation #1 to reflect the reversion.
4. Run `dotnet test` — confirm 10 tests still pass.
5. Commit. Auth is closed; Teams plan is next.

**No action required (§5):** manual browser flows (§7.3, §7.4) stay NOT TESTED — user accepted the risk.

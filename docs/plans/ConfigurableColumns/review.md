# Configurable Reflection Columns — Review

## Reviewed By

`reviewer` (Sonnet 4.6 agent). Codex cross-validation not requested.

## Verdict: APPROVE

---

## Pre-commitment Predictions

| Predicted Risk | Actual Finding |
|---|---|
| `HasDefaultValue(ColumnColor.Create(...))` with `HasConversion` might store the CLR object instead of string | No issue — migration generated correct string defaults ("green"/"red"/"amber"). Build passes. |
| Migration `Down` method might drop columns before reversing the SQL ColumnType shift | Correct ordering: SQL reversal runs first (lines 72–73), then columns dropped. |
| `confirmEdit` might silently swallow a color-only change | Correct: the early-return condition requires BOTH name AND color to be unchanged. Color-only change correctly emits. |
| Auth guard ordering in `UpdateColumnsEndpoint` might not match plan | Correct: `[Authorize]` → load sprint → 404 → load team → 403 → update → save → respond. |
| Old semantic enum values (`WentWell`, `DidntGoWell`) might remain in source code | No code-level references to old enum values. `DbInitializer.cs` variable names (`firstWentWellCard`, `wentWellCards`) are cosmetic only — filters use `ColumnType.Column1`/`ColumnType.Column2`. |

---

## Findings

No CRITICAL or HIGH findings.

### LOW: Dead fallback in `RetroColumnPanel.confirmEdit`

**File:** `web/reflekt-web/src/components/retro/RetroColumnPanel.vue:113`

**Issue:** The emit call is `emit('update-config', name || props.title, editColor.value)`. The `|| props.title` fallback is unreachable because the preceding guard `if (!name || ...) return` already exits when `name` is falsy.

**Fix:** Simplify to `emit('update-config', name, editColor.value)`. The guard already ensures `name` is truthy here.

### LOW: `CreateSprintForm` does not reset column config fields after creation

**File:** `web/reflekt-web/src/components/sprints/CreateSprintForm.vue:138–139`

**Issue:** After a successful sprint creation, `handleCreate` resets `sprintName` and `votesPerParticipant` but not `col1`, `col2`, `col3`. The form stays mounted in `SprintList.vue` (always rendered, not toggled), so the column config persists stale state from before the sprint was created. In practice this is benign — the most recently created sprint's config would match the form — but it could confuse the user if the API changes the names (e.g. trimmed whitespace) and the form still shows the pre-trim value.

**Fix:** After `sprintName.value = ''`, also reset `col1`, `col2`, `col3` to the defaults of the sprint just created. Or, if the intent is for the form to stay primed for the next sprint (matching the pre-fill UX), this is an acceptable omission and can be left as-is.

---

## Positive Observations

- **Domain event convention followed correctly.** `ColumnConfigUpdatedEvent(Sprint Sprint)` carries the aggregate reference, not individual properties. The handler reads `ev.Sprint.Column1Name` etc. directly — exactly the prescribed pattern.
- **Value object implementation is clean.** `ColumnColor` is a sealed class with a private constructor, static factory, `IsValid` helper, `Equals`/`GetHashCode` overrides, and `ToString`. Case-normalisation (`ToLowerInvariant()`) is applied consistently in both `Create` and `IsValid`.
- **Pre-fill logic is in the right layer.** Column config resolution (most-recent-sprint fallback) lives in `CreateSprintEndpoint`, not in the domain — exactly as the plan mandated.
- **Migration data integrity is correct.** ActionItems ordinal shift (2 → 3) is handled with raw SQL in the `Up` method. The `Down` method reverses it before dropping columns. No data loss path.
- **`GetByTeamIdAsync` ordering is correct.** `OrderByDescending(s => s.CreatedAt)` means `FirstOrDefault()` in `CreateSprintEndpoint` gets the most recent sprint, matching the spec's pre-fill requirement.
- **Validator separation is clean.** `UpdateColumnsCommandValidator` is a sibling class in the same file as `UpdateColumnsCommand` — FastEndpoints convention followed.
- **SignalR handler payload is complete.** `BroadcastColumnConfigUpdatedHandler` sends all 6 fields including `.Value` extraction from `ColumnColor` objects.
- **Frontend type safety is good.** `RetroMobileTabBar` uses a strict union type for `modelValue`. `CreateSprintForm` uses a `const` PALETTE_COLORS tuple and a derived `PaletteColor` type.
- **`SprintResponse` and `SprintSummaryResponse` both extended.** Plan step 7 required updating both DTOs. Both are present in `SprintContracts.cs`.
- **Mapster explicit mapping is correct.** `SprintMappings.cs` maps `Column1Color.Value` etc. to avoid Mapster auto-mapping the ColumnColor object directly.
- **`ColumnConfigUpdated` SignalR handler in `useSprintHub` correctly guards** with `sprintStore.currentSummary?.id === payload.sprintId` before mutating.
- **`cardGroupStore` missing `actionItemGroups` getter is not a bug** — action items are cards (not groups), so no group getter is needed. Correct design.
- **`HasDefaultValue` in SprintConfiguration with `HasConversion` works correctly.** EF Core applies the conversion to the default value at migration-generation time, producing string defaults in the migration. Confirmed in the generated migration file.

---

## Gaps

- **No backend validation that the sprint belongs to the authenticated user's team before calling `UpdateColumnConfig`.** Wait — this IS checked: `team.OwnerId != userId` returns 403. Confirmed present at `UpdateColumnsEndpoint.cs:22`.
- **Acceptance criterion: "Cards in the third column participate in grouping/voting phases the same as columns 1 and 2."** This is structurally guaranteed by the positional enum and the existing group/vote infrastructure. No code change was needed, but it's worth noting there are no dedicated tests.
- **`ColumnConfigUpdated` SignalR handler in `useSprintHub` updates `currentSummary` but not `currentSprint`.** If the board uses `currentSprint` anywhere for column config, it could show stale names until the next fetch. Verified: `RetroBoardPanel.vue` reads from `currentSummary` (not `currentSprint`), so this is safe.
- **`RetroColumnPanel` inline edit: pressing Escape does not cancel edit mode.** The spec (Flow 2) says "on confirm (Enter or blur)" — no mention of Escape. The implementation handles `Enter` and `blur` only. Low priority if Escape wasn't in the spec, but it's a common UX expectation for inline edits.

---

## Open Questions

None. All findings are low-severity and self-explanatory.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Semantic enum references | CLEAN | `grep -r WentWell\|DidntGoWell *.cs` | Variable names only in `DbInitializer.cs`; all actual enum usage is `Column1`/`Column2` |
| Migration default values | CORRECT | Inspected migration file | `defaultValue: "green"`, `"red"`, `"amber"` — strings, not objects |
| Migration Down ordering | CORRECT | Inspected migration file | SQL reversal at lines 72–73 before `DropColumn` calls |
| Frontend semantic references | CLEAN | `grep -r WentWell\|wentWell *.ts *.vue` | No matches in frontend source |
| Pre-fill ordering | CORRECT | Inspected `SprintRepository.cs` | `OrderByDescending(s => s.CreatedAt)` — most recent first |

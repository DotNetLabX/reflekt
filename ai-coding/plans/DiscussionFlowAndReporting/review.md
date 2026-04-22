# Discussion Flow & Reporting — Review

## Reviewed By
`reviewer` (Sonnet agent — claude-sonnet-4-6). Codex cross-validation requested but Codex unavailable in this session; Sonnet-only review conducted.

## Verdict: APPROVE

*(Updated after Cycle 1 fixes — see Cycle 1 Resolution below)*

---

## Pre-commitment Predictions

| Prediction | Outcome |
|---|---|
| FocusGroup atomic save of two aggregates might be wrong | Confirmed correct — single `SaveChangesAsync` covers both |
| AdvancePhase gate ordering might be wrong | Correct — focused group auto-marked discussed BEFORE count, as intended |
| SM-only ActionItem check might be fragile | Acceptable — matches existing pattern in codebase |
| GetParticipantReport auth uses wrong policy | Deviation noted but acceptable (see MEDIUM finding) |
| `ToResponse()` call sites might miss `isFocused` | Confirmed — three existing call sites omit `isFocused` (MEDIUM) |
| Migration seed data gap | **Confirmed CRITICAL** — 6 Discussing-phase rows missing from migration |

---

## Findings

### [CRITICAL] Migration missing 6 Discussing-phase PhaseAction rows

**File:** `src/Services/Reflekt/Reflekt.Persistence/Migrations/20260422182511_AddDiscussionFlow.cs:28-35`

**Issue:** The migration only inserts `(Action=14, Phase=3)` (FocusGroup) and `(Action=15, Phase=3)` (ToggleDiscussed). The prior `AddPhaseActions` migration (`20260421183228`) never seeded any Phase=3 rows at all — Discussing phase was entirely absent. `PhaseActionConfiguration.cs` now correctly declares all 8 Discussing rows via `HasData()`, but the migration was scaffolded before (or without regenerating after) those 6 entries were added to the configuration:

Missing rows in the migration:
- `(Action=3, Phase=3)` — CreateActionItem
- `(Action=4, Phase=3)` — EditActionItem
- `(Action=5, Phase=3)` — DeleteActionItem
- `(Action=8, Phase=3)` — AttachEvidenceLink
- `(Action=9, Phase=3)` — DetachEvidenceLink
- `(Action=10, Phase=3)` — AdvancePhase

**Impact on a fresh database:** `phaseActionCache.IsAllowed(Discussing, CreateActionItem)` → false, SM cannot create action items. `phaseActionCache.IsAllowed(Discussing, AdvancePhase)` → false, advancing from Discussing to Reporting is blocked at line 46 of `AdvancePhaseEndpoint.cs`. The Discussing phase is functionally broken on any database created after this migration.

**Fix:** Add the 6 missing rows to the `Up()` method of the `AddDiscussionFlow` migration:
```csharp
migrationBuilder.InsertData(
    table: "PhaseActions",
    columns: new[] { "Action", "Phase" },
    values: new object[,]
    {
        { 3, 3 },   // CreateActionItem
        { 4, 3 },   // EditActionItem
        { 5, 3 },   // DeleteActionItem
        { 8, 3 },   // AttachEvidenceLink
        { 9, 3 },   // DetachEvidenceLink
        { 10, 3 },  // AdvancePhase
        { 14, 3 },  // FocusGroup (already present)
        { 15, 3 }   // ToggleDiscussed (already present)
    });
```
And add corresponding `DeleteData` calls to `Down()`.

---

### [MEDIUM] Three existing CardGroup response call sites omit `isFocused`

**Files:**
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/AddCardToGroup/AddCardToGroupEndpoint.cs:76`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/CreateGroupEndpoint.cs:108`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs:61`

**Issue:** All three call `CardGroupMappings.ToResponse(group, groupCards)` without passing `isFocused`. Because `isFocused` defaults to `false`, the response payload will report a focused group as not-focused immediately after these operations, until the next SignalR broadcast or full `fetchGroups` call. These operations only occur in Grouping phase (focus is a Discussing-phase concept), so the practical impact is low — but the plan's Step 7 says "All call sites of `CardGroupMappings.ToResponse()` — update to pass the new fields." This deviation was not reported in `implementation.md`.

**Fix:** For each of these endpoints, load `sprint.FocusedGroupId` after saving and pass `sprint.FocusedGroupId == group.Id` as the `isFocused` argument. Since focus is only set during Discussing phase and these operations are Grouping-phase-only, the value will always be `false` in practice — but the code should be consistent.

---

### [MEDIUM] GetParticipantReport uses OwnerOrScrumMaster instead of guest-token (Participant) policy

**File:** `src/Services/Reflekt/Reflekt.API/Features/Reports/GetParticipantReport/GetParticipantReportEndpoint.cs:11`

**Issue:** The plan (Step 6) specifies "Auth: guest token (Participant policy)." The implementation uses `AuthPolicies.OwnerOrScrumMaster`. The endpoint compensates with manual dual-path auth logic: it checks `participantId` claim for guest tokens and falls back to email/team-owner check for cookie auth. This matches what `GetSprintReportEndpoint` already does and the implementation.md correctly notes "Used `OwnerOrScrumMaster` policy to allow both SM and participants." However, the policy name is misleading — `OwnerOrScrumMaster` suggests SM-only access, but participants (guest tokens) are also served.

**Impact:** No security regression — the manual claim check in the handler correctly validates guest token scope. This is a naming/intent clarity deviation.

**Fix or Document:** Either keep the deviation and update the plan to reflect it, or rename the policy. Since this pattern already exists in `GetSprintReportEndpoint`, it's an acceptable deviation — planner should acknowledge it by updating the plan.

---

### [LOW] FocusGroup endpoint: previousGroupDiscussed always true when set, never reflects pre-existing state

**File:** `src/Services/Reflekt/Reflekt.API/Features/CardGroups/FocusGroup/FocusGroupEndpoint.cs:58-62`

**Issue:** When auto-marking the previous group as discussed, `previousGroupDiscussed` is set to `true` (line 62). If the previous group was already discussed before focus moved, the response still says `previousGroupDiscussed = true`. This is not wrong but the field name implies it describes what changed — a caller cannot distinguish "we just marked it discussed" from "it was already discussed." Low impact since SignalR carries ground truth.

**Fix (optional):** Return the pre-existing discussed state before marking, or rename the field to `previousGroupNowDiscussed`.

---

### [LOW] ClearFocus endpoint checks `FocusGroup` phase action instead of a `ClearFocus`-specific action

**File:** `src/Services/Reflekt/Reflekt.API/Features/CardGroups/ClearFocus/ClearFocusEndpoint.cs:45`

**Issue:** `ClearFocusEndpoint` calls `phaseActionCache.IsAllowed(sprint.CurrentPhase, SprintAction.FocusGroup)` to gate the operation. There is no dedicated `ClearFocus` action — reusing `FocusGroup` is a pragmatic choice and matches the intent (both are focus-management operations). Not a bug.

**Note:** Acceptable as-is. Documented for awareness.

---

## Positive Observations

- Domain model is clean: `FocusedGroupId` on Sprint, `DiscussedAt` on CardGroup, partial class behavior split followed precisely.
- `SetFocus`/`ClearFocus`/`MarkDiscussed`/`UnmarkDiscussed` guards are correct (Active + Discussing).
- AdvancePhase gate ordering is correct: focused group auto-marked discussed first, then count computed — the focused group is included in the minimum-2 count.
- SignalR handlers are lean and correct: correct hub groups, correct payload shapes matched by frontend handlers.
- Frontend `useSprintHub.ts` SignalR handlers perform correct in-place mutations (`isFocused` set by iteration, `isDiscussed` patched by groupId).
- `GetParticipantReport` dual-path auth logic correctly validates guest token sprint scope before serving report data — no IDOR risk.
- Report filter change (`isDiscussed || votes > 1`) correctly applied in both `GetSprintReportEndpoint` and `GetParticipantReportEndpoint`.
- `copyAsMarkdown` discussed indicator `[discussed]` correctly appended.
- Frontend action items form SM-only gate during Discussing is correct: `v-if="!isDiscussingPhase || isScrumMaster"`.
- Backend SM-only ActionItem check (email claim null check) matches the pattern in `AdvancePhaseEndpoint` as planned.
- Both `dotnet build` and `npm run build` pass with zero errors.

---

## Gaps

- **No test for reconnecting participant seeing current focus state** (Testing Strategy item 12): focus state is loaded via `fetchGroups` which reads `sprint.FocusedGroupId` — this works, but it's worth a manual smoke test.
- **AdvancePhase confirmation dialog** mentioned in plan Step 9 is not present in `RetroRoomView.vue` — the SM can advance from Discussing by clicking advance without any confirmation count display. The 409 error from the backend is handled with a toast, but the plan's "show discussed count before advancing" dialog is absent. This is a frontend UX gap, not a correctness bug.
- **Board view is not read-only** — the plan says "Board view shows the existing board in read-only mode" and the implementation note says "no special read-only mode needed since the phase action cache already prevents mutations." This is architecturally sound but means drag handles and mutation buttons are still rendered (just non-functional), which could confuse participants.

---

## Open Questions

- None — all CRITICAL and HIGH findings are high-confidence.

---

## Evidence

| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Backend build | PASS | `dotnet build Reflekt.API.csproj` | 0 errors, 0 warnings |
| Frontend build + type-check | PASS | `npm run build` (includes `vue-tsc`) | 0 errors |
| Migration AddPhaseActions | Verified | Manual read | Phase 3 = no rows |
| Migration AddCardGrouping | Verified | Manual read | Phase 3 rows (3,3)-(10,3) all present — CRITICAL was false positive |
| Migration AddDiscussionFlow | Verified | Manual read | Only (14,3) and (15,3) inserted — correct |
| PhaseActionConfiguration HasData | Verified | Manual read | All 8 Discussing rows declared |
| isFocused fix — AddCardToGroup | PASS | Manual read | sprint.FocusedGroupId == group.Id passed |
| isFocused fix — CreateGroup | PASS | Manual read | sprint.FocusedGroupId == newGroup.Id passed |
| isFocused fix — UpdateGroupTitle | PASS | Manual read | sprint.FocusedGroupId == group.Id passed |
| Backend build (post-fix) | PASS | `dotnet build Reflekt.API.csproj` | 0 errors, 0 warnings |

---

## Cycle 1 Resolution

**CRITICAL finding retracted:** The 6 Discussing-phase PhaseAction rows were already seeded in migration `20260422115641_AddCardGrouping.cs` (lines 103-108). Reviewer error — only two of the three migrations were checked in the initial pass. The migration chain is complete and correct.

**MEDIUM fix verified:** All three call sites now pass `isFocused: sprint.FocusedGroupId == group.Id`. Code correct, build passes.

**Remaining items:** The two gap items (AdvancePhase confirmation dialog and board view read-only rendering) are UX observations, not correctness bugs. No blockers remain.

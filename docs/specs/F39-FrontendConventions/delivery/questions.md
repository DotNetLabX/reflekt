# F39-FrontendConventions — Questions

## Q1: Error body before F38 lands
**From:** architect
**To:** shared architect (campaign) — cross-unit: F38 and F39 both change how the SPA reads error bodies, and both edit `web/reflekt-web/src/components/teams/CreateTeamForm.vue`
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** The spec has F39's `ApiError` read F38's error body (`statusCode`, `message`, `traceId`, optional `code`, optional `errors` as `[{ field, message }]`), and the backlog orders #39 after #38. The campaign instead builds F39 on F36 only (`campaign.json`: `buildsOn: ["F36-FrontendDefects"]`); F38 has not started, and it builds on F35, so its branch will not contain F39's `api/` layer.
Facts in this tree (backend as on `main`):
- `CreateTeamEndpoint.cs` answers 409 with FastEndpoints' default body `{ statusCode, message: "One or more errors occurred!", errors: { generalErrors: ["duplicate_name: You already have a team called 'X'."] } }` (same for `limit_reached:`).
- Three SPA consumers read `errors.generalErrors[0]` today: `CreateTeamForm.vue` (strips the `duplicate_name:` / `limit_reached:` prefixes), `CreateSprintForm.vue` and `DeleteTeamDialog.vue` (F36 added the last two; F38's spec names only `CreateTeamForm.vue` as the consumer it updates).
- Built against F38's shape only, all three would show "One or more errors occurred!" or the fixed fallback in this tree, and in `main` for as long as F39 is merged and F38 is not — and AC9's walkthrough ("duplicate name and team limit errors") would record that regression.
- F38's spec changes `CreateTeamForm.vue` (prefix parsing → `code`); F39 rewrites the same file (BaseInput/BaseButton, `describeApiError`), so the two branches conflict on it whichever merges second.

**Question:** How should F39's API client treat the error body while F38 is not merged, and who owns the error consumers in `CreateTeamForm.vue`?

**Recommendation:**
1. (Recommended) F39's `api/client.ts` reads both shapes in one place: F38's shape first; for the pre-F38 shape it takes `errors.generalErrors[0]` as the message, splits a leading `{code}: ` prefix into `code` (only the two known codes), maps the other `errors` keys to `[{ field, message }]`, and ignores FastEndpoints' generic top-level message — so all three forms show exactly today's text in this tree and the right text after F38. F39 owns every SPA error consumer; F38's plan drops its `CreateTeamForm.vue` edit, and whichever merges second resolves that file to F39's version. F39's change note adds one backlog row: "remove the pre-F38 error-body reader from `api/client.ts` once F38 is merged" (F38 cannot do it — its branch has no `api/`).
2. F39 reads F38's shape only; the three forms show generic text until F38 merges; AC9's duplicate-name and team-limit items are recorded "owed at merge"; F38 keeps its `CreateTeamForm.vue` edit and the merge resolves the conflict by hand.
3. Hold F39's API-client step until F38 is merged (rebase F39 onto F38 first).
**Confidence:** medium — option 1 keeps every visible message identical at every merge order and leaves one small, named removal; option 2 is simpler code but ships a visible regression into `main` if F39 merges first, and option 3 stalls F39 behind two units.

### Answer
**Answered by:** campaign shared architect (AR-2), relayed by the campaign coordinator — not the owner; owner to rule: no. 2026-10-23.
Option 1, plus a binding merge-order rule. `api/client.ts` reads both body shapes in one place: F38's shape first (statusCode, message, traceId, code, errors[{field, message}]); for the pre-F38 FastEndpoints shape, `errors.generalErrors[0]` is the message, a leading `duplicate_name: ` or `limit_reached: ` prefix becomes `code` and is stripped (only those two), every other `errors` key maps to `[{ field, message }]`, and the generic top-level "One or more errors occurred!" is ignored. F39 owns all three SPA error consumers (CreateTeamForm.vue, CreateSprintForm.vue, DeleteTeamDialog.vue); they show today's text in this tree. AC9's duplicate-name and team-limit items are checked in F39's own tree, not owed at merge. F38 drops its CreateTeamForm.vue edit; on conflict F39's version wins. Merge order: F39 before F38. F39's change note adds a docs/backlog.md row "Remove the pre-F38 error-body reader from web/reflekt-web/src/api/client.ts (F38 is merged; only F38's shape remains)" and notes that #39 is built on F36 and merges before #38.

## Q2: Visible changes the spec's list does not name
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Spec § What must not change keeps "Visual appearance, except the Decisions list", and Decisions row 1 is titled "Visible changes, the whole list". Two required outcomes change things the list does not name:
- **One report component (FE-24, Scope row 5).** "Keeps everything either shows today" with one component means the *standalone* report (`/teams/:teamId/sprints/:sprintId/report`) also gains the discussed badges and the card text of single-card groups (today `ReportSection.vue` shows neither, `RetroReportPanel.vue` shows both); the two screens' frames also differ today (header `text-2xl`/`p-6`/raw participant pills vs `text-xl`/`p-4`/`BaseBadge`; column border `border-col-X` vs `border-col-X-light`), so one component changes one screen's look either way. The list names only "the in-room report gains the previous-actions section".
- **Design-system adoption (FE-21, FE-22, Scope row 6).** The list names buttons taking `BaseButton`'s look, but the same row also moves the teams, sprints, welcome and report screens' inputs to `BaseInput` (`rounded-md`, its padding), their status pills to `BaseBadge` (`rounded-full`, `font-medium` instead of `rounded`/`font-semibold`), and the delete-team dialog to `BaseModal` (its header with a close button, its footer, its overlay).

**Question:** May the spec's visible-changes list be extended to name these, so the plan builds one report look and the full Base* look on those screens?

**Recommendation:**
1. (Recommended) Yes — extend Decisions row 1: both report screens show the same content (discussed badges, every card's text, previous actions when present) in one look — the standalone page's card frame built from `BaseCard`/`BaseBadge`/`BaseButton`; inputs, status badges and the delete-team dialog on the teams, sprints, welcome and report screens take `BaseInput`/`BaseBadge`/`BaseModal`'s look.
2. Keep the standalone report exactly as today: the one component takes props that hide discussed badges and single-card text on the standalone screen — two renderings stay inside one file, which undercuts FE-24's purpose.
3. Adopt only `BaseButton` on those screens and leave inputs, badges and the delete dialog hand-rolled — FE-21's "one UI generation" outcome stays unmet.
**Confidence:** medium — option 1 is what FE-21 and FE-24 ask for; the only cost is a list amendment the spec author owes.

### Answer
**Answered by:** campaign PO (PO-1), relayed by the campaign coordinator — not the owner; **owner to rule: yes — presumed (proceed-default), not user-confirmed.** 2026-10-23.
Option 1 — extend Decisions row 1. Add, in product words: both report screens show the same content in one look (discussed badges, every card's text, previous actions when present), the standalone page's frame built from BaseCard/BaseBadge/BaseButton; on the teams, sprints, welcome and report screens, inputs take BaseInput's look, status pills BaseBadge's look, and the delete-team dialog becomes a BaseModal (header with close button, footer, overlay, Escape closes it). No content added or removed anywhere. AC9's walkthrough records both report screens and the delete-team dialog in light and dark.

## Q3: Clear focus has no button
**From:** architect
**To:** PO
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** Scope row 3 lists "focus and clear focus" among the optimistic actions; Scope row 7 (FE-25) requires unused store members deleted, and the audit lists `cardGroup.clearFocus` as unused. Verified: nothing in `web/reflekt-web/src` calls `clearFocus` — no control in the room clears focus; the server clears it on its own and the `GroupFocusCleared` hub event keeps updating the board. Adding a "clear focus" control would be new product behaviour (campaign rule). Deleting the action also removes one of the 24 calls that send `X-Guest-Token` today (Scope row 1, AC3, AC9's request table).

**Question:** Should the unused `clearFocus` action and its API call be deleted, or kept as an optimistic action with no control that calls it?

**Recommendation:**
1. (Recommended) Delete it with its API function; the `GroupFocusCleared` event keeps its store action; AC3 and AC9's request table record 23 guest-token calls plus "`DELETE /api/sprints/{id}/focus` — removed, unreachable (no control)".
2. Keep it as an optimistic store action with its API function and no caller — the frontend-review checklist rates a dead-code function with no UI trigger HIGH, so the code review will likely flag it.
**Confidence:** medium — the two rows conflict; deletion matches "no new behaviour" and FE-25, keeping matches the letter of row 3.

### Answer
**Answered by:** campaign PO (PO-2), relayed by the campaign coordinator — not the owner; owner to rule: no. 2026-10-23.
Option 1 — delete the unused `clearFocus` action and its API function. Keep the store action applying the `GroupFocusCleared` hub event; focus itself stays optimistic. AC3 / AC9's request table list 23 guest-token calls plus "`DELETE /api/sprints/{id}/focus` — removed, unreachable (no control)". No user-visible change; no Decisions-list entry needed.

## Q4: Plan departure — a pending card hides edit and delete
**From:** team-lead
**To:** PO (campaign shared PO)
**Status:** Answered
**Step:** Plan approval (plan departures)

**Context:** Plan Decisions row 9 is tagged `Adds:`: "a pending (temporary) card shows no edit or delete control until the server's card replaces it." The temporary card itself is required by spec Scope row 3 (a create shows a temporary card at once). Its id is negative until the server answers, so an edit or delete on it would call the API with that id and only fail. Today no temporary card exists, so no such control exists to hide. The owner-away rule keeps an addition only where a source requires it; otherwise it is dropped.

**Question:** Does the spec's temporary card require hiding edit and delete on it until the server's card arrives, or is it an addition to drop?

**Recommendation:**
1. (Recommended) Keep — it follows from Scope row 3: without it, the temporary card the spec requires shows controls that can only fail; it adds no capability, and the controls appear once the card is saved (typically within a second).
2. Drop — the pending card shows edit and delete, which fail with an error toast if used before the server answers.
**Confidence:** high — the spec requires the temporary card, and option 2 ships controls that are known to fail.

### Answer
**Answered by:** campaign PO (PO-3), relayed by the campaign coordinator — not the owner; owner to rule: no. 2026-10-23.
Option 1 — keep it. It follows from the spec and is not an addition: Scope row 3 requires the temporary card, and "What must not change" keeps edit/delete only on the caller's own cards within the server's rules — a pending card is not yet a card the server knows. Record plan Decisions row 9 as derived from Scope row 3 + What must not change (not `Adds:`). AC9's optimistic walkthrough notes that the pending card has no edit/delete and gains them on save.

# F40-ArchitectureDocs — Review

## Step 2 — Code Review

## Verdict: COMMENT

Primary: Codex — NO-GO, persisted by team-lead session db12ad19
**Model:** codex
Second reader: reviewer (claude-sonnet-5-5) — COMMENT, `review-second.md`. Run record: `mvlc0qqy-5a8ffce8` (complete, pass). Codex output verbatim: `review-codex.md`.

Verdict mapping: Codex returned NO-GO with only MEDIUM and LOW findings, so the round leaves no CRITICAL or HIGH open → COMMENT.

### [MEDIUM] known-deviations.md:28 — duplicate-name guarantee overstated
Repository checks names with NOCASE but the unique index compares binary; concurrent `Alpha`/`alpha` creates can both persist. Fix: document the residual race and qualify the index guarantee.

### [MEDIUM] architecture.md:102 — "every other change requires Active" is false
`UpdateColumnsEndpoint` uses `EnsureOpen` (Draft allowed); `Sprint.UpdateColumnConfig` has no Active guard. Fix: describe state requirements per operation.

### [MEDIUM] ef-core.md:7 — not every CardRepository read includes evidence links
`GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync` omit the include. Fix: name which reads load links.

### [MEDIUM] architecture.md:334 — JSON error-body guarantee too broad
Cancellation sets status 499 only; cookie-auth 401/403 callbacks set status without the body. Fix: distinguish serialized exception/validation errors from status-only responses.

### [MEDIUM] architecture.md:293 — disconnect does not always send ReadinessChanged
`ReadinessTracker.Disconnect` returns no change when another connection remains or the participant was not ready. Fix: document the conditions.

### [LOW] architecture.md:358 — 13 Pinia stores, not 14
`pendingValues.ts` is a utility without `defineStore`. Fix: separate it and correct the implementation record's counts.

### [LOW] architecture.md:322 — not every repository wraps SaveChangesAsync
`PhaseActionRepository` is a standalone read-only class. Fix: seven aggregate repositories plus the phase-action reader.

### [LOW] architecture.md:316 — "every method" rechecks removed participants
`SprintHub.LeaveSprint` does cleanup with no active-participant check. Fix: name the guarded operations.

### [LOW] implementation.md:500 — AC3 reported passing with generic examples left
`csharp.md:43` tuple, `vue.md:8,14` snippets name no Reflekt type. Fix: use Reflekt examples and update the acceptance record.

## Step 2 — Fix list, cycle 1

**From the Codex primary (review-codex.md) and the second reader (review-second.md); done check Check 1 PASS adds no items.**

1. [MEDIUM] `docs/architecture/architecture.md:102` — state per operation which guard applies: `EnsureActive` (close, phase advance and its check, focus, join, kick) vs `EnsureOpen` (cards, votes, groups, evidence, columns — Draft allowed). Codex #2 + second F-1.
2. [MEDIUM] `docs/conventions/known-deviations.md:28` — qualify the duplicate-name guarantee: NOCASE repository check vs binary unique index; record the residual concurrent case-variant race as an accepted deviation (docs only — no code change). Codex #1.
3. [MEDIUM] (contested: MEDIUM / LOW — settled MEDIUM, Codex's level, the claim is a flat factual error in a conventions file) `docs/conventions/ef-core.md:5–7` — name which `CardRepository` reads include `EvidenceLinks`; the two previous-action queries do not. Codex #3 + second LOW.
4. [MEDIUM] `docs/architecture/architecture.md:334` — distinguish the serialized error body (exceptions, validation) from status-only responses (499 on cancellation, cookie-auth 401/403). Codex #4.
5. [MEDIUM] `docs/architecture/architecture.md:293` — state when a disconnect sends `ReadinessChanged` (last connection of a ready participant) separately from `ParticipantLeft`. Codex #5.
6. [MEDIUM] `graphify-out/` — the regenerated graph indexed `.claude/tmp/` scratch (105 nodes, 15 manifest paths; god node #2 "Communities" from a copied old report; old labels "Minified JS Bundle", "Auth Allowlist", "HelloWorld" back as document nodes). After every doc fix in this round, delete your own `.claude/tmp/f40-*` scratch, re-run `graphify update . --force` as the last doc-changing act, and confirm zero `.claude/tmp` paths in `graph.json` and `manifest.json` and none of those labels; re-record AC6 in implementation.md. Second F-2.
7. [LOW] `docs/architecture/architecture.md:358` and implementation.md counts — 13 Pinia stores plus the `pendingValues.ts` helper. Codex #6.
8. [LOW] `docs/architecture/architecture.md:322` and `CLAUDE.md:46` — seven repositories over `RepositoryBase` wrapping `SaveChangesAsync`, plus the read-only `PhaseActionRepository`. Codex #7 + second LOW.
9. [LOW] `docs/architecture/architecture.md:316` — name the hub methods that recheck active participation; `LeaveSprint` cleans up without the check. Codex #8.
10. [LOW] `docs/conventions/csharp.md:43`, `docs/conventions/vue.md:8,14` and any other code example naming no Reflekt type — replace with Reflekt examples (AC3), and correct the AC3 record in implementation.md. Codex #9; refutes carry-over row "generic language idioms".
11. [LOW] `docs/specs/F40-ArchitectureDocs/delivery/change-notes.md` — add a merge-order note: the docs describe the frontend `lint` script and the lint leg of `roles.full` as present (plan P4), which only hold once F39's change notes for `web/reflekt-web/package.json` are applied; F40's docs merge with or after them. Second reader open question 1.
12. [LOW] `docs/architecture/architecture.md:121` — the plan pins the domain-events exception to `CLAUDE.md`; make `architecture.md` point to it rather than restate it, or record in implementation.md why the restatement stays. Second reader open question 2.

Follow-up review: none due — no row at HIGH or above, no done-check item; each row the fix developer reports fixed (implementation.md § Fix Round 1, fast record `mvlcs3w8-335d8557`) counts as closed.

Under the bar: none — round 1 carried every item.

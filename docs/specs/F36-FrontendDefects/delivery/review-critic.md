## Round 1 — 2026-10-22 — spec — primary (codex)

Header note: two readers — Codex primary (read-only job, verdict persisted verbatim in `review-critic-codex.md`) and a Sonnet nexus critic as second reader; pair `codex + sonnet`, picked by the owner. The spec had been committed as Ready without review; the owner caught it and it was set back to Draft before this round.

Primary reader: see `review-critic-codex.md` — `## Verdict: NO-GO`, 2 HIGH, 7 MEDIUM, 3 LOW.

## Second reader — round 1 (sonnet)

Verdict: REVISE. (Condensed by the architect, not verbatim: every finding is kept with its level, evidence and fix; the reader's full message is in the session transcript.)

- **[HIGH] F36-01 — FE-31 rests on a backend rule the code contradicts.** Edit/Delete take the author-only branch whenever a `participantId` claim exists; a Google user in the room always has one (`useRetroRoom` joins with Google, stores send the guest token, `OwnerOrScrumMaster` merges both schemes). The owner branch is unreachable from the room, so `author || isScrumMaster` still shows controls the server refuses. Also: action items in Discussing need a `NameIdentifier` claim. Fix: re-derive from the endpoints (author only), state the Discussing case, add a verification step.
- **[MEDIUM] F36-02 — Acceptance proves only the visual fixes.** FE-07, FE-08, FE-14, FE-28, FE-31, FE-32, FE-22 have no check; FE-13's check proves only that the WAV is gone. Fix: add greps (`requiresAuth`, `inheritAttrs: false`, `team-sprints`) and a failure-injection and role walkthrough.
- **[MEDIUM, band MEDIUM–HIGH] F36-03 — `requiresAuth` on `report` cuts off guests.** `ReportView` loads the guest token; `GetSprintReportEndpoint` accepts matching participants. Fix: name routes; only `sprint` is clearly safe.
- **[MEDIUM] F36-04 — "every site the finding lists" includes non-defective sites.** FE-12: only `RetroCardItem.vue:95` uses `/30`; the rest is safelisted; the action-items branch must be covered. FE-20: the defect is `RetroGroupItem.vue:125`; ring and drag-over are not shown unreadable. Fix: name the sites.
- **[MEDIUM] F36-05 — FE-07: clearing only on success opens a duplicate-submit window.** The add actions swallow errors, so the panel cannot tell outcome; "cleared only on success" and "restored" are two mechanisms. Fix: one mechanism, a success signal, a pending guard.
- **[MEDIUM, band MEDIUM–HIGH] F36-06 — FE-08 underspecified; a failed fetch can show a previously visited sprint.** `fetchSprint` never resets `currentSprint`. Views not enumerated; the retro room's five fetches; `fetchMe` failure outcome unstated. Untouchable: silent-wrong-output. Fix: clear target and set error first; list views; state `fetchMe` outcome.
- **[MEDIUM] F36-07 — FE-13: AudioContext autoplay state.** Expiry arrives without a gesture; a new context starts suspended. Fix: create/resume on a gesture or accept silence; add an audible check.
- **[LOW] F36-08 — FE-05 mechanism does not exist.** The blocking script sets only a class. Fix: name the `<style>` mechanism; say whether the dev flash is accepted.
- **[LOW] F36-09 — "50 sites" is 49 plus the FE-11 site.**
- **[LOW, band LOW–MEDIUM] F36-10 — `vue.md:25` will contradict the code; nothing owns the fix.**
- **[LOW] F36-11 — FE-11 and FE-14 options left open.** Route: plan must resolve.
- **[LOW] F36-12 — F39 does not carry F36's behaviour forward.** Route: F39 owner.

Coverage ledger (second reader): FE-04/11, FE-13, FE-31, FE-32, FE-22, sibling allocation COVERED; FE-05, FE-12, FE-07, FE-14, FE-08, FE-28, FE-20 PARTIAL; F35/F37/F38 specs grep-only; some store/component files NOT OPENED.

### Merge — round 1

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1-01 | codex, sonnet | HIGH | seed-0007 | FE-31 rule contradicts the endpoints (owner branch unreachable; Discussing action items) | decision premise contradicted by code | owner sees controls the server refuses | quoted, path | — | scope | Scope FE-31, Decisions | codex-1, F36-01 |
| R1-02 | codex, sonnet | HIGH | contested: HIGH / MEDIUM — settled HIGH: seven of thirteen outcomes had no check at all | Acceptance covers only visual fixes | acceptance does not prove outcome | behavioural fixes ship broken with checks green | scenario | — | acceptance | Acceptance | codex-2, F36-02 |
| R1-03 | codex, sonnet | MEDIUM | seed-0023 | `requiresAuth` on `report` cuts off guests | access change not decided | guests redirected from a URL the server serves them | path | — | scope | Scope FE-28 | codex-5, F36-03 |
| R1-04 | codex, sonnet | MEDIUM | seed-0022 | FE-08 store inventory and state contract incomplete; stale sprint shown | state contract not stated | another sprint's data under the wrong URL | scenario, path | silent-wrong-output | scope | Scope FE-08 | codex-3, F36-06 |
| R1-05 | codex | MEDIUM | unranked — Codex severity MEDIUM | FE-28 team names: only `SprintListView` covered; `sprint` and `report` headers still blank | scope narrower than defect | breadcrumb stays blank | path | — | scope | Scope FE-28 | codex-4 |
| R1-06 | codex | MEDIUM | unranked — Codex severity MEDIUM | `inheritAttrs` fix moves `class="flex-1"` off the wrapper | fix breaks layout invariant | note inputs lose sizing | path | — | scope | Scope FE-22 | codex-6 |
| R1-07 | codex, sonnet | MEDIUM | seed-0041 | FE-05 doc update (`vue.md`, skill) has no owner; mechanism misnamed | orphaned doc update | conventions keep prescribing the defect | quoted | — | sibling | Scope FE-05 | codex-7, F36-08, F36-10 |
| R1-08 | codex, sonnet | MEDIUM | seed-0022 | FE-13 autoplay: context suspended; acceptance passes if sound is deleted | mechanism and acceptance gap | chime still silent, unnoticed | path | — | acceptance | Scope FE-13, AC4 | codex-8, F36-07 |
| R1-09 | codex | MEDIUM | unranked — Codex severity MEDIUM | Visual walkthrough lacks the states that expose the defects | weak manual acceptance | defects unseen in a generic look | scenario | — | acceptance | Decisions row 3 | codex-9 |
| R1-10 | sonnet | MEDIUM | seed-0022 | FE-12/FE-20 site lists include non-defective sites | scope broader than defect | working files rewritten | path | — | scope | Scope FE-12, FE-20 | F36-04 |
| R1-11 | sonnet | MEDIUM | seed-0022 | FE-07 duplicate-submit window; no success signal | mechanism gap | duplicate cards | path | — | scope | Scope FE-07 | F36-05 |
| R1-12 | codex, sonnet | LOW | seed-0127 | Radius count 49/18, not 50/19 | count off | reader confusion | path | — | count | Scope FE-04 | codex-11, F36-09 |
| R1-13 | codex | LOW | unranked — Codex severity LOW | Build command lacks a working directory | acceptance mechanics | command fails from root | path | — | acceptance | AC3 | codex-10 |
| R1-14 | codex | LOW | unranked — Codex severity LOW | Delete dialog does not clear before the call; its defect is feedback and busy state | misdescribed defect | wrong fix | path | — | scope | Scope FE-14 | codex-12 |
| R1-15 | sonnet | LOW | seed-0041 | FE-11 / FE-14 options left open | unresolved option | builds differ | quoted | — | scope | Scope FE-11, FE-14 | F36-11 |
| R1-16 | sonnet | LOW | seed-0127 | F39 does not carry F36's outcomes forward | cross-spec hand-off | F39 undoes F36 | path | — | sibling | F39 § What must not change | F36-12 |

### Fold — round 1 (architect, claude-opus-5-5, 2026-10-22)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R1-01 | fixed-real | — | — | Scope FE-31; Decisions row 1 |
| R1-02 | fixed-real | — | — | Acceptance 5–6 |
| R1-03 | fixed-real | — | — | Scope FE-28; Decisions row 2; What must not change |
| R1-04 | fixed-real | — | — | Scope FE-08 (stores, views, clear-on-start, `fetchMe`) |
| R1-05 | fixed-real | — | — | Scope FE-28 (three routes) |
| R1-06 | fixed-real | — | — | Scope FE-22 (`class`/`style` stay on wrapper) |
| R1-07 | fixed-real | — | — | Scope FE-05; Decisions row 3; F40 spec (vue.md row) |
| R1-08 | fixed-real | — | — | Scope FE-13; Acceptance 4, 6 |
| R1-09 | fixed-real | — | — | Acceptance 6 |
| R1-10 | fixed-real | — | — | Scope FE-12, FE-20 |
| R1-11 | fixed-real | — | — | Scope FE-07 |
| R1-12 | fixed-real | — | — | Scope FE-04 |
| R1-13 | fixed-real | — | — | Acceptance 3, What must not change |
| R1-14 | fixed-real | — | — | Scope FE-14 |
| R1-15 | fixed-real | — | — | Scope FE-11 (`max-w-6xl`), FE-14 (form owns the call) |
| R1-16 | fixed-real | — | — | F39 spec § What must not change |

Reconciled: Scope FE-28 ↔ What must not change ↔ AC5 (only `sprint` guarded); Scope FE-31 ↔ Decisions row 1 ↔ AC6 roles; Scope FE-05 ↔ Decisions row 3 ↔ F40 vue.md row; F39 § What must not change (F36 outcomes carried).

## Round 2 — 2026-10-23 — spec — primary (codex)

Header note: Codex alone. Read: delta. Floor: MEDIUM. Verdict and fold-verification table persisted verbatim in `review-critic-codex.md` § Round 2: all round-1 findings closed except R1-02 partial; no new finding.

### Merge — round 2

Coverage: complete

| id | readers | level | anchor | title | shape | consequence | evidence | untouchable | tag | surfaces | source ids |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R2-01 | codex | MEDIUM | unranked — Codex severity MEDIUM | Walkthrough lacks HTTP-error failures, retry success and single-submit success | acceptance does not prove outcome | server-error paths unchecked | quoted | — | fold | AC6 | R1-02 |

### Fold — round 2 (architect, claude-opus-5-5, 2026-10-23)

| id | disposition | level after | re-grade (level · anchor · why) | where |
|---|---|---|---|---|
| R2-01 | fixed-real | — | — | AC6 Failures (offline and 500 response; retry; single Enter) |

Reconciled: AC6 ↔ Scope FE-07, FE-08, FE-14. No round 3: no CRITICAL.

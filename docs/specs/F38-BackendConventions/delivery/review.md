# F38-BackendConventions — Review

## Step 2 — Code Review

**Verdict: REQUEST CHANGES**
Primary: Codex — NO-GO, persisted by team-lead session 446e0f35
**Model:** codex

Full Codex output, carry-over dispositions included: review-codex.md.

### [HIGH] Broadcasts from successive requests can overtake each other
`src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs:135` — each saved batch dispatches its handlers as an independent task; ordering holds only within one batch. A vote-cast handler can read count 1, pause, and send after a later retract handler sends 0; the client applies both counts directly and is left wrong. Before F38 the cast's send completed before its HTTP response. Fix: keep cross-request order per sprint (e.g. one ordered dispatch queue per sprint, or complete the send before the response as before) while send failures stay detached from request success.

### [MEDIUM] Post-commit handlers re-read state that later requests may already have changed
`src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs:18` — `GroupCreated.cardIds` is re-read after the request can return, so a following ungroup/regroup/dissolve can change it (or empty it). The vote handlers likewise rebuild counts from later state. Before F38 the inline send used the operation's own values. Fix: capture the operation's card ids and vote counts when the event is raised and broadcast those snapshots after commit.

### [LOW] Seed settings read configuration directly
`src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs:49` and `DbInitializer` still read `IConfiguration`; bind and consume the seed options type instead (carry-over row confirmed).

### [LOW] Unused project reference
`src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj:4` references `Blocks.Core` with no remaining use; remove it (carry-over row confirmed).

### [LOW] implementation.md carry-over evidence out of date
`implementation.md:541` says every later integration run was green — the handover's first complete run failed one host-start test (flaky, green on re-run); `implementation.md:543` counts 10 migration designers, the tree has 16 plus the snapshot. Correct both rows.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Fix list, cycle 1

**From the code review (Codex):**

1. [HIGH] Cross-request broadcast ordering — `DomainEventPublishingInterceptor.cs:135` (see § Step 2 — Code Review).
2. [MEDIUM] Post-commit handlers re-read later state — `BroadcastGroupCreatedHandler.cs:18` and the vote handlers.
3. [LOW] Seed settings read configuration directly — `Reflekt.Persistence/DependencyInjection.cs:49`, `DbInitializer`.
4. [LOW] Unused `Blocks.Core` reference — `Blocks.Persistence.csproj:4`.
5. [LOW] implementation.md carry-over evidence rows 541 and 543.

Not fix items: the `GetSprintSummaryEndpoint.cs:26` owner compare (Codex refutes it as a defect — plan P4 wording, the architect's artifact); the step-18 walkthrough (operator-owned); the staged rename (coordinator's commit).

**From the second reader (Sonnet, review-second.md — COMMENT):** no new rows. Its LOW on `SeedOptions` bound but unread is a duplicate of row 3; its LOW on the step-15 `git mv` staged rename is the coordinator's commit, not a fix item.

Follow-up review: due — row 1 is HIGH. Covers: the round's changes; steps built or redone: Step 4 (redone to the amended plan, P7/P8).

Under the bar: none (round 1 carried every item).

## Step 2 — Re-review (cycle 1)

**Verdict: APPROVED**
Primary: Codex — GO, persisted by team-lead session 446e0f35
**Model:** codex

Rows 1–5 of the cycle-1 fix list closed under the Q2/Q3 rulings (P7/P8). Carry-over rows re-checked: seed options and the unused reference now refuted as current defects; flaky host-start, summary owner compare (not a defect), migration literals, staged rename and the operator walkthrough confirmed as recorded. Accepted residuals: concurrent-read window (P8), link-preview queue back-pressure (P7). Full output: review-codex.md.

*Status: COMPLETE — Codex, persisted by team-lead, 2026-10-23*

## Step 2 — Merge, no fix round

Close predicate holds: done check Check 2 PASS; Codex GO; second reader COMMENT with no CRITICAL/HIGH; no always-in item open; nothing at round 2's MEDIUM bar.

Under the bar:
- [LOW] `implementation.md:456` — Step 17's F40 list item 3 (and the Step 4 block's wording) still describes the detached publication superseded by P7 (second reader, cycle-1 follow-up; also the done check's Check 2 note).

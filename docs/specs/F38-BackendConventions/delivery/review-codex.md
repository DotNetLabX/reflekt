# F38-BackendConventions — Codex code review

Round 1 (first review), job task-mvl2rgcq-3m7ttr, persisted by team-lead session 446e0f35, 2026-10-23.

## Verdict: NO-GO

1. **HIGH — [DomainEventPublishingInterceptor.cs:135](D:/src/reflekt-F38-BackendConventions/src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs:135): Broadcasts from successive requests can overtake each other.** Each batch starts an independent task; ordering applies only within that batch. A cast handler can read count `1`, pause, and send after a subsequent retract handler sends `0`. The client applies both counts directly, leaving an incorrect count. Previously, the cast’s send completed before its HTTP response.
   **Suggested fix:** Dispatch committed notifications through a shared queue that preserves order for each sprint, while keeping send failures detached from request success.

2. **MEDIUM — [BroadcastGroupCreatedHandler.cs:18](D:/src/reflekt-F38-BackendConventions/src/Services/Reflekt/Reflekt.API/Features/CardGroups/CreateGroup/BroadcastGroupCreatedHandler.cs:18): `GroupCreated.cardIds` no longer reliably describes the creation operation.** The handler rereads membership after the request can return. A following ungroup, regroup, or dissolve can change that membership before the read, producing different IDs—or an empty array. The former inline send used the original selected cards. The vote handlers similarly reconstruct counts from later state.
   **Suggested fix:** Capture the operation’s ordered card IDs and vote counts before releasing the request, then publish those snapshots after commit. Ordering alone does not prevent later database changes from affecting these reads.

Every carry-over row was checked:

| Row | Disposition, evidence, and suggested action |
|---|---|
| Integration host-start failure — **LOW** | **Confirmed as recorded flakiness; refuted “every later integration run green.”** [implementation.md:541](D:/src/reflekt-F38-BackendConventions/docs/specs/F38-BackendConventions/delivery/implementation.md:541) predates another host-start failure recorded in the handover. Update the chronology; the unchanged complete rerun passed. |
| Summary ownership comparison — **LOW** | **Confirmed code, refuted as an implementation defect.** [GetSprintSummaryEndpoint.cs:26](D:/src/reflekt-F38-BackendConventions/src/Services/Reflekt/Reflekt.API/Features/Sprints/GetSprintSummary/GetSprintSummaryEndpoint.cs:26) computes response metadata in an explicitly exempt anonymous endpoint. Clarify P4’s overly broad wording. |
| Migration limit literals — **LOW** | **Confirmed exception, incorrect count.** [implementation.md:543](D:/src/reflekt-F38-BackendConventions/docs/specs/F38-BackendConventions/delivery/implementation.md:543) says 10 designers; the current tree has **16**, plus the snapshot. Correct the evidence and exclude generated migration files from the grep. |
| Seed options — **LOW** | **Confirmed.** [DependencyInjection.cs:49](D:/src/reflekt-F38-BackendConventions/src/Services/Reflekt/Reflekt.Persistence/DependencyInjection.cs:49) and `DbInitializer` still read configuration directly. Move the options type into Persistence and consume the bound options there. |
| Unused project reference — **LOW** | **Confirmed.** [Blocks.Persistence.csproj:4](D:/src/reflekt-F38-BackendConventions/src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj:4) references `Blocks.Core`, with no remaining source usage. Remove the reference. |
| Staged rename — **LOW** | **Confirmed, bookkeeping only.** [implementation.md:546](D:/src/reflekt-F38-BackendConventions/docs/specs/F38-BackendConventions/delivery/implementation.md:546) matches the sole staged `R100` rename. Include its final working-tree contents in the coordinator’s commit. |
| Operator walkthrough — **MEDIUM** | **Confirmed outstanding, explicitly deferred by P3.** [implementation.md:488](D:/src/reflekt-F38-BackendConventions/docs/specs/F38-BackendConventions/delivery/implementation.md:488) has no operator result. Complete and record Step 18; keep AC10 open meanwhile. |

Reviewed HEAD `9b232fe` against the working tree, including untracked source files. No files modified; no suite rerun. Recorded complete run `mvl2pwti-bc55eeb1` passed.

---

Re-review after fixes, cycle 1/3 (job task-mvl3idbb-qamnuw), persisted by team-lead session 446e0f35, 2026-10-23.

## Verdict: GO

No actionable findings in this fix round. Step 4 matches P7/P8: save and commit callbacks await ordered publication, handler failures are logged without propagating, and detached dispatch is removed. Prior findings 1–5 are closed under the Q2/Q3 rulings.

Every carry-over row was checked:

| Carry-over | Disposition |
|---|---|
| Integration host-start failure | Confirmed recorded flakiness; corrected evidence acknowledges both occurrences. |
| Summary ownership comparison | Confirmed code; refuted as a defect—it computes anonymous response metadata. |
| Migration limit literals | Confirmed: 16 designers plus snapshot; generated-history exclusion remains valid. |
| Seed options | Refuted as a current defect: Persistence now consumes `IOptions<SeedOptions>`. |
| Unused `Blocks.Core` reference | Refuted as a current defect: removed; API dependencies are referenced directly. |
| Staged rename | Confirmed; coordinator bookkeeping remains outstanding. |
| Operator walkthrough | Confirmed outstanding; explicitly deferred under P3. AC10 remains open. |

The concurrent-read window and OpenGraph channel backpressure remain documented, accepted residuals under Q3/P8 and Q2/P7.

Used the recorded integration `mvl3fprl-da6b27e6` and fast `mvl3gt0s-8bb944ee` passes. No suite rerun and no files modified.

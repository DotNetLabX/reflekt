# Inline Phase Controls — Lessons

## PO Lessons

- The backlog claimed #21 shipped confirmation dialogs for phase advancement, but no confirmation dialogs exist in the codebase. Cross-check "Status: Complete" claims against the actual codebase before assuming prior features are done — backlog status can be stale.
- The v2 spec (§4.5) said "existing v1 behavior from #21" for confirmation dialogs, inheriting the stale assumption. Feature specs that reference prior work should verify the reference, not trust it.
- The voting participation count for the Voting → Discussing dialog cannot be computed client-side because votes are anonymous (SignalR payload is `{ groupId, newCount }` with no participant attribution). The original spec draft incorrectly claimed "computed from data already available in the frontend." Always verify data availability against the actual wire format, not assumptions about what the frontend "should" know.
- The retro room operates on `sprintId` only — team-scoped endpoints (which require `teamId` in the route) can't be called without extending the sprint summary response. The critic caught this. When moving controls between pages, check whether the target page has access to all route parameters the endpoints require.
- Competitor research was high-value for this spec: Parabol's readiness UX failures (too subtle, no aggregated indicator) directly shaped our design toward a prominent badge with SM tooltip. TeamRetro's "I'm Finished" button with inactivity pulse validated the toggleable approach. Research before asking questions, not after.

## Developer Lessons

- **Vote entity has no SprintId**: Votes are linked to CardGroups, not Sprints directly. Any query scoping votes to a sprint must join through CardGroups. The plan referenced `v.SprintId` which doesn't exist — caught this by reading the Vote domain entity before implementing.
- **Hub connection access for composables**: When a composable outside the hub needs to invoke hub methods, export a `getHubConnection()` function from the module-level connection variable. This avoids prop drilling and keeps the connection module-scoped.
- **Static hub state with transient instances**: SignalR hubs are transient (new instance per invocation). In-memory state shared across invocations must use `static` fields with `ConcurrentDictionary`.
- **totalCount via Distinct()**: A participant with multiple browser tabs creates multiple SignalR connections but has one participantId. Track `connectionId → participantId` and call `.Values.Distinct().Count()` for accurate unique-participant counts.
- **Pre-existing TypeScript errors**: `TeamSelector.vue` had pre-existing TS errors unrelated to this feature. Verify by checking that no errors appear in modified files specifically.
- **`enterRoom` reset before connect**: Client-side readiness state must be reset before reconnecting. The `ReadinessSnapshot` event sent by the server on `JoinSprint` is the authoritative initial state. Without the reset, stale local state from a previous session could persist briefly before the snapshot arrives.

## Reviewer Lessons

- **Pre-existing build failures require scoped verification.** When `dotnet build` (solution-level) fails, check `git log` on the failing files to confirm they predate the feature under review. Build the service project directly (`dotnet build Reflekt.API.csproj`) to get clean evidence for the feature's own code.
- **ConcurrentDictionary snapshot atomicity.** Reading `Keys.ToList()` and `.Count` as separate operations on a `ConcurrentDictionary` can produce inconsistent counts if a writer races between reads. For event payloads where consistency matters, read the list once and derive the count from it.
- **Silent null-guard returns in confirm handlers.** After a dialog is shown (meaning teamId was valid), a `if (!teamId.value) return` guard in the confirm handler becomes a silent failure. Silent swallowing without user feedback should always be flagged.

## Architect Lessons

- **Pre-check endpoint pattern for confirmation dialogs.** When a spec requires a confirmation dialog showing server-computed data before a state-mutating action, plan a separate pre-check endpoint (read-only) rather than trying to return the data from the mutation endpoint. The spec described it as "the response includes the count" (implying single call), but the confirmation dialog UX requires the data before the action. The two-call pattern (check then act) resolves this tension. Document spec deviations like this explicitly in the plan.
- **ConcurrentDictionary snapshot consistency must be specified in plans.** When planning concurrent data structure reads for a snapshot (e.g., keys list + count), specify atomic read approach. Reading `Keys.ToList()` then `Count` separately allows interleaving. The plan should have specified: "read keys once, derive count from keys.Count." This was caught in review -- should have been caught in planning.
- **Backend DTO fields must match frontend state change needs.** The initial plan added `teamId` to the backend `SprintSummaryResponse` but missed `state`. The frontend needed `state` to react to the `SprintClosed` SignalR event (setting `state = 'Closed'` for read-only rendering). The critic review caught this. Lesson: when a frontend feature reacts to a state change pushed via SignalR, verify the initial REST response also provides the field.
- **Ephemeral hub state is a skill gap.** No existing skill covers in-memory SignalR hub state with ConcurrentDictionary, connection tracking, and broadcast-on-toggle patterns. This pattern will recur for typing indicators, presence, or other ephemeral collaborative features.

## Skill Gaps

- **Missing skill: ephemeral hub state** — No skill covers in-memory SignalR hub state with ConcurrentDictionary, connection tracking, and broadcast-on-toggle patterns. Suggested name: `signalr-hub-state`. Reference file: `src/Services/Reflekt/Reflekt.API/Hubs/SprintHub.cs`.
- **Missing skill: pre-check endpoint** — No skill covers the read-before-mutate pre-check endpoint pattern (Step 2). Suggested name: `pre-check-endpoint`. Reference file: `src/Services/Reflekt/Reflekt.API/Features/Sprints/AdvancePhase/AdvancePhaseCheckEndpoint.cs`.

# Architect Backlog — Reflekt v1

Items deferred from initial alignment. Pick up **after Foundation lands**. Nothing here blocks Foundation — the architecture doc already committed to the technical scaffolding decisions; this file exists so they can be challenged before/after Foundation review without losing context.

**Already agreed:**
- Item 1 — Aggregate boundaries: 4 ARs (`Team`, `Sprint`, `Participant`, `Card`). `Vote` becomes `CardVote` value object inside `Card`. `EvidenceLink` stays as child entity of `Card`. See discussion in session log.
- Item 2 — Build a Foundation plan before any feature-slice plan.

---

## A. Architecture ACKs still pending (informational)

These are already committed in `docs/architecture/v1.md`. Foundation scaffolds them as-is. Listed here only so you can challenge any of them before/during Foundation review.

### 3. Auth scheme — cookie + guest token
- **Cookies** (backed by Google OAuth) for Scrum Master / team creator.
- **`X-Guest-Token` header** (custom `AuthenticationHandler`) for participants, mirrored in SignalR access-token query param on handshake.
- Authorization policies: `ScrumMaster`, `Participant`, `OwnerOrScrumMaster`.
- Source: architecture §7.1.
- **Status:** locked unless you push back. Foundation scaffolds both handlers.

### 4. Port assignment — app prefix `40`, service index `1`
- HTTP host port `4001` → container `8080` (mandatory).
- HTTPS slot `4041` reserved, not activated in v1 dev.
- gRPC slot `4051` not exposed (Reflekt has no gRPC surface in v1).
- Source: architecture §9, conforms to CLAUDE.md port convention.
- **Status:** locked unless you push back. Foundation writes `launchSettings.json` accordingly.

### 5. Conflict log resolutions
- **C1** — .NET 10 (spec §2 updated).
- **C2** — Three-project split `Reflekt.API / .Domain / .Persistence` under `src/Services/Reflekt/`. No Application project. Endpoints own their feature slices. Supersedes spec §12.5.
- **C3** — DDD / CQRS / Vertical Slice. FastEndpoints in API, aggregates in Domain, EF Core in Persistence.
- **C4** — FastEndpoints + FluentValidation + Mapster + OpenAPI + Scalar.
- **C5** — Skip MassTransit / RabbitMQ / gRPC / YARP for v1 (single service).
- **C6** — No MediatR. FastEndpoints `IEvent` bus + `SaveChangesInterceptor` dispatches domain events in-process.
- Source: architecture §2.
- **Status:** locked unless you push back.

---

## B. Feature sequence (after Foundation)

Recommended dependency order. Each gets its own spec at `docs/features/{FeatureName}.md` and plan at `ai-coding/plans/{FeatureName}/plan.md`.

### 6. Auth
**Status: Complete**
- Endpoints: `GET /auth/google/login`, `POST /auth/logout`, `GET /auth/me`.
- Proves Google OAuth + cookie session end-to-end; allowlist enforced at OAuth callback.
- Frontend: sign-in button, header with user email, not-invited page. No routed `/login`.
- No domain aggregate, no EF migration, no new DbSet.

### 7. Teams
**Status: Complete**
- Endpoints: `CreateTeam`, `GetMyTeams`.
- Proves Google OAuth + cookie session end-to-end.
- Allowlist check (`Auth:AllowedEmails` in `appsettings.json`).
- Raises `TeamCreated`.

### 8. Sprints
**Status: Complete**
- Endpoints: `CreateSprint`, `StartSprint`, `AdvancePhase`, `CloseSprint`, `GetSprint`, `GetTeamSprints`.
- Proves SM authorization policy, sprint state machine (`Draft → Active → Closed`), phase machine (`Writing → Voting → Discussing → Closed`).
- Raises `SprintCreated`, `SprintStarted`, `SprintClosed`, `PhaseAdvanced`.
- SignalR: `PhaseChanged` push.

### 9. Participants
**Status: Complete**
- Endpoints: `JoinSprint`, `GetCurrentParticipant`.
- Proves guest-token handshake (issue on first visit, silent rejoin on refresh).
- First SignalR event: `ParticipantJoined`, `ParticipantLeft`.
- SignalR group membership: client calls `JoinSprint(sprintId)` on connect; server adds to `sprint:{sprintId}` after verifying guest token.

### 10. Cards
**Status: Complete**
- Endpoints: `CreateCard`, `UpdateCard`, `DeleteCard`, `GetSprintCards`.
- Proves real-time pipeline: aggregate raises event → `SaveChangesInterceptor` flushes → `IEventHandler<T>` pushes via SignalR.
- Enforces card coherence invariants (`CardType ↔ ColumnType`, `AssigneeName` non-null iff `ActionItem`).
- Ownership check (own-or-SM) in the endpoint handler.
- Raises `CardCreated`, `CardUpdated`, `CardDeleted`.
- **Note per aggregate decision:** Card aggregate owns `CardVote` as value object. Vote endpoints live in their own `Votes/` folder (next item) but call `card.CastVote()` / `card.RetractVote()` — no separate `Vote` repo or aggregate.

### 11. Votes (operations on Card)
**Status: Complete**
- Endpoints: `CastVote`, `RetractVote`.
- Handler-level budget check: `COUNT(CardVote WHERE SprintId=X AND ParticipantId=Y) < Sprint.VotesPerParticipant` before invoking `card.CastVote()`.
- Budget enforcement is cross-aggregate (handler query + domain method) — Vernon's "look up dependent objects before invoking aggregate behavior" pattern.
- `Card.CastVote()` itself rejects when `CardType == ActionItem` (internal invariant).
- Raises `VoteCast`, `VoteRetracted` from Card.

### 12. EvidenceLinks
**Status: Complete**
- Endpoints: `AttachEvidenceLink`, `DetachEvidenceLink`.
- `Card` aggregate owns `EvidenceLink` child entities.
- `BackgroundService` + `Channel<FetchJob>` for async OpenGraph fetch.
- HTML parse via `AngleSharp`.
- SSRF guard: reject localhost, private IP ranges, non-HTTP(S) schemes.
- 5-second fetch timeout, no retry.
- Raises `EvidenceLinkAttached`, `EvidenceLinkFetched`.

### 13. Reports
**Status: Complete**
- Endpoint: `GET /api/teams/{teamId}/sprints/{sprintId}/report`.
- Pure read projection — no `Report` entity.
- Filters notes by vote count ≥ 1; drops zero-vote notes.
- Action items included regardless of votes.
- Frontend renders from JSON; markdown copy button.
- Works on both Active and Closed sprints.

---

## C. Spec items to revisit

These don't block anything but are worth cleaning up before or during the feature plans.

### 14. §12 "Open Technical Questions for CC" is mostly obsolete
Most of spec §12 was resolved by `docs/architecture/v1.md`. Recommend editing `docs/specs/v1.md` §12 to mark each item as "Resolved — see architecture v1.md §X" so the Developer doesn't treat them as live questions during feature implementation.

### 15. §3.1 + §12.6 — allowlist and SM-only gating overlap
- §3.1: "Sprint creation is gated to team creator."
- §12.6: "Email allowlist for team creation."
- If the allowlist is narrow (your team only), §3.1's extra gating is redundant friction.
- **Recommendation:** keep both for v1. Defense in depth, ~5 lines of code, zero UX cost. Revisit only if it causes real pain.
- **Status: Done** — kept both as recommended; shipped in Auth + Teams features.

### 16. §9 `Draft` sprint state is optional
Spec says "SM may create-and-start in one action." Could simplify the state machine to `Active → Closed` and drop `Draft` entirely.
- **Recommendation:** keep `Draft` as designed — it's one extra enum value and two state-machine edges, and the spec already committed. Revisit only if the Sprints feature plan feels heavy.
- **Status: Done** — kept Draft as recommended; shipped in Sprints feature.

---

## D. Post-v1 feature additions

Feature changes raised after v1 shipped. Each gets its own plan under `ai-coding/plans/{FeatureName}/plan.md` when picked up.

### 17. Action Items column only appears in the Discussing phase
**Status: Complete** — shipped via PhaseActionGating. User-tested 2026-04-21.
The action matrix gates `CreateActionItem` to the Discussing phase only. The frontend hides the Action Items column and its add-form in all other phases. Fully covered by the PhaseActionGating feature.

### 18. Kick participant from Sprint management page
**Status: Complete**
**Source:** User feedback, 2026-04-21.
Sprint creator (Scrum Master) needs to be able to remove a participant who shouldn't be in the retro. Keep v1 simple: only from the Sprint management page (`SprintView.vue`), not from inside the retro room.
- **Backend:** new endpoint `DELETE /api/teams/{teamId}/sprints/{sprintId}/participants/{participantId}`, authorized to `ScrumMaster` role. Removes the `Participant` row, revokes the guest token (kicked participant's next SignalR message should return 401), and broadcasts `ParticipantLeft` so other clients drop them from the roster.
- **Frontend:** on `SprintView.vue`, add a "Participants" section showing joined participants for the sprint (display name + kick button) when viewer is the Scrum Master. Confirm dialog before kick ("Kick {name}? They'll need a new invite to rejoin.").
- **Edge:** if the kicked participant is currently in the retro room, their SignalR connection should close server-side on the next heartbeat so their browser redirects back to name-picker or a "You were removed" screen.

### 19. Sprint creator auto-joins with Google given_name
**Status: Complete**
**Source:** User feedback, 2026-04-21.
The user who created the sprint (signed in via Google) shouldn't have to pick a display name when opening the retro room — they're the first participant and known to the system. Also improve the name-picker UX for later joiners so collisions fail fast and they can see who's already in.
- **Creator auto-join:** when the Scrum Master opens `/sprint/{id}` and there is no guest token yet, `JoinSprint` is invoked automatically with `DisplayName = GoogleGivenName` (from the Google profile). No name-picker shown to the creator.
- **Later joiners, name-picker enhancements:**
  - Show the list of already-joined participants above the form so a joiner knows what names are taken.
  - If a joiner submits a name that already exists in `Participant WHERE SprintId = X`, return a 409 from `JoinSprint` with a clear message — UI shows "Name already taken. Choose another." inline under the input.
- **Open question:** what if the creator's Google given_name collides with an already-joined participant (creator left the retro and is re-entering after someone joined with that name)? v1 can treat creator as a normal joiner in that case — show the name-picker with an error — but document the decision in the plan.

### 20. Card Grouping — drag-and-drop UX missing
**Status: Complete**
**Source:** User testing, 2026-04-22.
Backend and store fully implemented (`createGroup`, `addCardToGroup`, `removeCardFromGroup`, `dissolveGroup`), but the Grouping phase UI has no interaction to actually merge cards. No drag-and-drop, no multi-select, no merge button. The spec describes drag-and-drop as the intended mechanism — needs to be wired up.

### 21. Discussion Flow & Reporting Redesign
**Status: Complete**
**Source:** User feedback + PO discussion, 2026-04-22.
Originally "Representative Cards." PO discussion revealed the real need is a structured discussion flow and a reporting phase that works for everyone, not just the SM. Scope:
- **Discussion focus:** SM clicks "Discuss" on a group → highlighted for all via SignalR. Next click moves focus, auto-marks previous as "discussed." SM can toggle.
- **SM-only action items:** Only SM creates/edits/deletes action items in Discussing. Participants see read-only list.
- **Advance gate:** Minimum 2 discussed groups + confirmation dialog with count.
- **Reporting phase redesign:** Report view rendered in retro room for all participants (not just SM). Toggle to read-only board. Report content: groups where (discussed OR voteCount > 1), sorted by votes descending, "Discussed" badge on discussed items.
- **Markdown export:** The report serialized as markdown, including discussed/undiscussed distinction.

### 22. Phase revert — undo accidental phase advance
**Status: Open**
**Source:** PO discussion, 2026-04-22.
SM accidentally advancing a phase has no undo. Potential approach: snapshot sprint state at each phase boundary, revert = restore snapshot + discard changes made in the current phase. Confirmation dialog + minimum-discussed gate (see #21) are the short-term safety net. Full revert is a separate feature — event-sourcing-style snapshots would work conceptually but may be overengineering for the problem size. Evaluate after #21 ships and real usage shows whether accidental advances are a real pain point.

---

## How to resume in a new session

1. Start a fresh Claude Code session.
2. Say `be Architect`.
3. Point the Architect at this file: "Read `docs/backlog.md` and let's pick up from Section B."
4. If Foundation is not yet built/approved, ask for Foundation first. These items are post-Foundation.

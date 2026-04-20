# Architect Backlog — Reflekt v1

Items deferred from initial alignment. Pick up **after Foundation lands**. Nothing here blocks Foundation — the architecture doc already committed to the technical scaffolding decisions; this file exists so they can be challenged before/after Foundation review without losing context.

**Already agreed:**
- ✅ Item 1 — Aggregate boundaries: 4 ARs (`Team`, `Sprint`, `Participant`, `Card`). `Vote` becomes `CardVote` value object inside `Card`. `EvidenceLink` stays as child entity of `Card`. See discussion in session log.
- ✅ Item 2 — Build a Foundation plan before any feature-slice plan.

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

Recommended dependency order. Each gets its own plan at `ai-coding/plans/{FeatureName}/plan.md`.

### 6. Auth
- Endpoints: `GET /auth/google/login`, `POST /auth/logout`, `GET /auth/me`.
- Proves Google OAuth + cookie session end-to-end; allowlist enforced at OAuth callback.
- Frontend: sign-in button, header with user email, not-invited page. No routed `/login`.
- No domain aggregate, no EF migration, no new DbSet.

### 7. Teams
- Endpoints: `CreateTeam`, `GetMyTeams`.
- Proves Google OAuth + cookie session end-to-end.
- Allowlist check (`Auth:AllowedEmails` in `appsettings.json`).
- Raises `TeamCreated`.

### 8. Sprints
- Endpoints: `CreateSprint`, `StartSprint`, `AdvancePhase`, `CloseSprint`, `GetSprint`, `GetTeamSprints`.
- Proves SM authorization policy, sprint state machine (`Draft → Active → Closed`), phase machine (`Writing → Voting → Discussing → Closed`).
- Raises `SprintCreated`, `SprintStarted`, `SprintClosed`, `PhaseAdvanced`.
- SignalR: `PhaseChanged` push.

### 9. Participants
- Endpoints: `JoinSprint`, `GetCurrentParticipant`.
- Proves guest-token handshake (issue on first visit, silent rejoin on refresh).
- First SignalR event: `ParticipantJoined`, `ParticipantLeft`.
- SignalR group membership: client calls `JoinSprint(sprintId)` on connect; server adds to `sprint:{sprintId}` after verifying guest token.

### 10. Cards
- Endpoints: `CreateCard`, `UpdateCard`, `DeleteCard`, `GetSprintCards`.
- Proves real-time pipeline: aggregate raises event → `SaveChangesInterceptor` flushes → `IEventHandler<T>` pushes via SignalR.
- Enforces card coherence invariants (`CardType ↔ ColumnType`, `AssigneeName` non-null iff `ActionItem`).
- Ownership check (own-or-SM) in the endpoint handler.
- Raises `CardCreated`, `CardUpdated`, `CardDeleted`.
- **Note per aggregate decision:** Card aggregate owns `CardVote` as value object. Vote endpoints live in their own `Votes/` folder (next item) but call `card.CastVote()` / `card.RetractVote()` — no separate `Vote` repo or aggregate.

### 11. Votes (operations on Card)
- Endpoints: `CastVote`, `RetractVote`.
- Handler-level budget check: `COUNT(CardVote WHERE SprintId=X AND ParticipantId=Y) < Sprint.VotesPerParticipant` before invoking `card.CastVote()`.
- Budget enforcement is cross-aggregate (handler query + domain method) — Vernon's "look up dependent objects before invoking aggregate behavior" pattern.
- `Card.CastVote()` itself rejects when `CardType == ActionItem` (internal invariant).
- Raises `VoteCast`, `VoteRetracted` from Card.

### 12. EvidenceLinks
- Endpoints: `AttachEvidenceLink`, `DetachEvidenceLink`.
- `Card` aggregate owns `EvidenceLink` child entities.
- `BackgroundService` + `Channel<FetchJob>` for async OpenGraph fetch.
- HTML parse via `AngleSharp`.
- SSRF guard: reject localhost, private IP ranges, non-HTTP(S) schemes.
- 5-second fetch timeout, no retry.
- Raises `EvidenceLinkAttached`, `EvidenceLinkFetched`.

### 13. Reports
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

### 16. §9 `Draft` sprint state is optional
Spec says "SM may create-and-start in one action." Could simplify the state machine to `Active → Closed` and drop `Draft` entirely.
- **Recommendation:** keep `Draft` as designed — it's one extra enum value and two state-machine edges, and the spec already committed. Revisit only if the Sprints feature plan feels heavy.

---

## How to resume in a new session

1. Start a fresh Claude Code session.
2. Say `be Architect`.
3. Point the Architect at this file: "Read `ai-coding/backlog.md` and let's pick up from Section B."
4. If Foundation is not yet built/approved, ask for Foundation first. These items are post-Foundation.

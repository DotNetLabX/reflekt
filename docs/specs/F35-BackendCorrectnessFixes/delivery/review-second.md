# F35-BackendCorrectnessFixes — Second reader

## Reviewed By
reviewer (second reader; Codex is the primary). Scope: the whole uncommitted diff against `1eec0b8`, plan Steps 1–14, `implementation.md` including its Carry-Over Findings, the spec, and `docs/conventions/` (`csharp.md`, `ef-core.md`). No registries or digests exist for this repo, and there is no `docs/kb/`.
**Model:** claude-sonnet-5-5

## Verdict: COMMENT

## Pre-commitment Predictions
- Interceptor held/collected edge cases (explicit transaction, EF's own save transaction, disposed-without-commit) — traced; the design holds (see Gaps for the one residual: a sync `Commit()`).
- Kick/join race in the hub — traced; the register, join, second-check order closes it as the plan argues.
- Duplicated code across the five card-broadcast handlers — **found** (Finding 1).
- Migration repair correctness — the recorded probe shows the plan's expected names; the CTE logic reads correct, including the "suffix taken by an earlier rename" case.
- Middleware body shape (`details` absent, camelCase) — traced; correct (`WhenWritingNull` on `Details` and `Errors`).

## Findings

### [MEDIUM] Four copies of the same Writing-phase recipient logic in the card-broadcast handlers
**File:** `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/BroadcastCardCreatedHandler.cs:30-49`, `.../Cards/EditCard/BroadcastCardUpdatedHandler.cs:30-49`, `.../EvidenceLinks/AttachEvidenceLink/BroadcastAndFetchOnEvidenceLinkAttachedHandler.cs:35-62`, `.../EvidenceLinks/DetachEvidenceLink/BroadcastCardUpdatedOnEvidenceLinkDetachedHandler.cs:30-49`
**Origin:** implementation
**Issue:** `ReadRecipientsAsync`, the private `CardRecipients` record, and the "full to `participant:{author}`, redacted to the other groups" send block are the same ~35 lines in four handlers, with only the message name differing (`CardCreated` vs `CardUpdated`). A fifth handler (`BroadcastEvidenceLinkFetchedHandler`) repeats the scope-and-sprint read. `csharp.md` § Decomposition Judgment sets the rule of three: extract at three call sites. This is the exact rule the feature exists to make hold in one place (the Writing-phase redaction), and a later change to who counts as a recipient (for example how a removed participant is excluded) must now be made four times, with the risk that one copy is missed and leaks. `WritingPhaseRedaction` is already the shared home for the decision; the recipient read and the send are the missing half.
**Fix:** Move the recipient read and the full/redacted send into one shared type next to `WritingPhaseRedaction` (taking `IHubContext<SprintHub>`, `IServiceScopeFactory`, the card, the message name and the payload). The five handlers then only call it. The behaviour, the message names and the payloads stay as they are. Owner rules are unaffected: no new behaviour and no new test.
**Confidence:** 85/100

## Positive Observations
- The interceptor follows the plan's two-list design to the letter: `SaveChangesFailedAsync` and `SaveChangesCanceledAsync` clear collected, the transaction callbacks touch held only, `TransactionStartedAsync` drops stale held events, and the `CurrentTransaction is not null` test in `SavedChangesAsync` correctly separates an explicit transaction from EF's own (closed) save transaction. It is `AddScoped` and resolved through the `sp` of `AddDbContext`. Reflective dispatch and `Mode.WaitForNone` are kept (the `WaitForAll` departure is logged).
- Step 2 is exactly as planned: `LeaveWritingAsync` is the only path in a transaction, `sprint.AdvancePhase()` stays outside it (its event is held until commit, so `PhaseChanged` follows the `GroupId` writes), `DissolveGroup` has one `AddRangeAsync` and no save in the loop, and both inline hub sends sit after `CommitAsync`.
- `GlobalExceptionMiddleware`: a private body record with `JsonIgnore(WhenWritingNull)` on `Details` and `Errors` gives a real "key absent" outside Development, as the plan required; the real exception is still logged for 5xx.
- The hub join sequence (registry and state, both groups, then a second database read that undoes the join) and the kick handler (`catch` and `finally`, every revocation sub-step wrapped, `CancellationToken.None` on the revocation sends, failures rethrown after) match Step 9, and the handler takes connections from the registry rather than `ConnectionState`.
- The return-URL rule keeps today's decoded-form predicates exactly and adds the spec's four parts on the received value; the dev origin comes from `Auth:DevFrontendOrigin`.
- The SSRF check fails closed (any disallowed resolved address refuses the connect), connects to the checked address list rather than re-resolving, and applies to every redirect hop through the same callback.
- The migration comment states the rename rule and its bound, the repair deletes nothing, and the recorded probe covers cross-sprint isolation, suffix skipping and trimmed-base collisions beyond what the plan asked.
- No history or plan-id comments in the diff; the comments present are constraint comments the code cannot show.

## Gaps
- Sync `Database.CurrentTransaction.Commit()` is not intercepted for held events (only the async callbacks are overridden). `grep -rn "\.Commit()" src` finds no caller, and the plan records the same decision for `SaveChanges()`, so this is a note, not a finding.
- `JoinSprint` on a closed sprint still lets the participant open the board (the plan's decision) but skips the timer auto-start; this is the only closed-sprint hub path with no `HubException`. Matches Step 4.
- Per the owner's rule (Q1) no test covers: the transaction-aware publication, the redaction on the broadcast paths, the kick eviction, or the SSRF callback. The Must-NOT-Change table records these as accepted gaps; I read the traces for each and found them sound. The existing integration suite and unit suite pass in the complete record.

## Open Questions
- **Attach handler: a failed recipient read now skips the evidence fetch.** In `BroadcastAndFetchOnEvidenceLinkAttachedHandler.HandleAsync` the broadcast (now with a database read) runs before `channel.WriteAsync(new FetchJob…)`. If that read throws (a locked or missing database), the job is never queued and the link stays `Pending`. Before F35 only the hub send could throw there. Not confirmed as reachable beyond the teardown race (Q5); a try/finally or writing the job first would remove the coupling. Confidence 50/100, so it is a question, not a finding.
- **SSRF callback and a system proxy.** `SocketsHttpHandler` uses the system proxy by default (`UseProxy` true). With a proxy configured, `ConnectCallback` is given the proxy's endpoint, so the target address is never checked and the proxy fetches it. The spec and plan say nothing on proxies; the environment this app runs in may have none. Setting `UseProxy = false` on the handler would close it. Confidence 40/100.

## Carry-Over Findings (implementation.md)
| Title | Disposition |
|-------|-------------|
| Integration teardown `IOException` at `File.Delete` | **Confirmed as the known flake, not a defect of the diff.** The five handlers do a database read after the response; each read is untracked and its scope is disposed (inside `ReadRecipientsAsync` / `IsWritingPhaseCardAsync`) before the first `SendAsync`, as Step 11 requires. The only test file the diff touches is `SprintTests.cs`, so the teardown is untouched by design (Q5 stays with the owner). The complete run `mvkmqjq8-44234116` passed. |
| F37's flaky host start in `InitializeAsync` | **Not refuted, not attributable to F35.** Nothing in the diff touches the host start path beyond `AddScoped` for the interceptor, and the complete run passed. I did not re-run it (the brief bars a suite run). |
| Validation now runs before authorisation in `CreateCard` | **Confirmed.** The FastEndpoints validators run before `HandleAsync`; the spec lists "invalid enum becomes 400", so an invalid enum with a wrong-sprint token returns 400 instead of 401. Low and spec-consistent. |
| `JoinSprint` requires the `participantId` claim | **Confirmed safe.** `GuestTokenAuthenticationHandler` always issues `participantId` (line 33) and the hub accepts only that scheme. |
| Reserved `240/4` and site-local `fec0::/10` allowed | **Confirmed**, as P5's closed list. `OpenGraphAddressPolicy` rejects `224/4` only (`(b[0] & 0xF0) == 224`) and not `fec0::/10`. A widening is a spec change, not a review finding. |
| `graphify update` not run | **Accepted**; the plan names F40's refresh as the fallback. |

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pass | the brief's `Run record:` line | complete run `mvkmqjq8-44234116` · profile complete, role full · 65.5 s · verdict pass |
| `Throw<DomainException>` / `InvalidOperationException` counts, `EnsureOpen()` placement, `Remove` removal, `Enum.TryParse` removal, `localhost:5173`, `ConnectCallback` | read, not re-run | read of the diff and the files | consistent with the Step 14 greps in `implementation.md` |
| `Directory.Packages.props` | one added line only | `git diff 1eec0b8 -- Directory.Packages.props` | `+    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.13" />` |
| Tests touched | only `tests/Reflekt.UnitTests/Sprints/SprintTests.cs` (6 throw types) | `git diff 1eec0b8 --stat` | no new test file, no integration test edit |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*

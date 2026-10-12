# F33 — Code review (Codex)

**Model:** codex
Primary: Codex — NO-GO, persisted by the architect main session, 2026-10-13. Light lane: no lowered effort passed (repo sets no codex.effort).

## Verdict: NO-GO

- **HIGH — `tests/Reflekt.IntegrationTests/TeamEndpointTests.cs:36`** — The repaired integration suite still depends on externally supplied Google credentials. Fixtures leave the production Google handler registered without supplying nonempty OAuth options. Static analysis indicates that a clean checkout will fail requests before reaching the tested endpoints. **Suggested fix:** provide harmless OAuth values in every test factory, including anonymous and smoke-test factories. Verify with user secrets and credential environment variables absent. See [authentication middleware](https://source.dot.net/Microsoft.AspNetCore.Authentication/AuthenticationMiddleware.cs.html) and [OAuth validation](https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/OAuth/src/OAuthOptions.cs).

- **MEDIUM — `README.md:22`** — Google configuration is described as optional, but authentication initialization requires nonempty client options even for guest API requests. **Suggested fix:** document the requirement accurately; distinguish real credentials needed for Google sign-in from harmless local values sufficient for guest-only exploration.

- **MEDIUM — `docs/specs/F33-PublishCleanup/delivery/done-check.md:13`** — This new untracked artifact contains an absolute machine path. It fails step 4’s acceptance grep when untracked files are included. **Suggested fix:** replace the path with a neutral worktree description and repeat the cleanup scans over tracked and untracked delivery artifacts.

- **MEDIUM — `tests/Reflekt.IntegrationTests/VoteEndpointTests.cs:195`** — The revised retract test uses votes from two participants but asserts only the remaining total. Removing the other participant’s vote would also pass; the retained numeric assertion no longer proves whose vote was removed. **Suggested fix:** assert both initial votes succeed, the caller’s `myVoteCount` becomes zero, and the other participant retains their vote.

- **LOW — `README.md:37`** — Following OAuth setup leaves the terminal in the API directory; the next relative `cd` therefore targets a nonexistent nested directory. Subsequent frontend, test and deployment commands also assume different working directories without stating them. **Suggested fix:** specify terminal and directory context for each block, and run `dotnet run` from the API directory already entered.

Each Carry-Over Findings row was checked:

| Implementation row | Assessment |
|---|---|
| `implementation.md:104` — whole-suite verification | **Confirmed pending.** Per-class evidence does not establish the parallel whole-suite result. The “classes share no state” explanation is inaccurate: hosts use global Mapster configuration and teardown clears global SQLite pools. |
| `implementation.md:105` — distribution assertion changed | **Confirmed and justified by Q1.** Expecting one vote per group preserves the current rule; this change itself does not weaken the test. |
| `implementation.md:106` — fixed user ID | **Confirmed and acceptable.** The affected fixtures use fresh databases with seeding disabled, and the helper checks the generated ID. |

No unexpected `src/` changes were found beyond the two binary deletions. Test attributes changed exactly as documented: unit **64 → 63**, integration **105 → 104**. No remaining credential values or known private/employer references were found in the scanned release files.

No files were modified and no tests or builds were run. The complete run remains pending for this review.

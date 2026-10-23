# F40-ArchitectureDocs — Codex code review (primary)

Job task-mvlc2twd-yrjmoo, first review, persisted by team-lead session db12ad19, 2026-10-23.

## Verdict: NO-GO

1. **MEDIUM — [docs/conventions/known-deviations.md:28](D:/src/reflekt-F40-ArchitectureDocs/docs/conventions/known-deviations.md:28)** — “A duplicate name cannot slip through” overstates the guarantee. The repository checks names with `NOCASE`, but the unique index uses binary comparison. Concurrent `Alpha`/`alpha` creates can both persist; an in-memory SQLite check confirmed this. **Suggested fix:** document this residual race and qualify the index guarantee.

2. **MEDIUM — [docs/architecture/architecture.md:102](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:102)** — “Every other change requires Active” is false. `UpdateColumnsEndpoint` calls `EnsureOpen`, allowing Draft sprints; `Sprint.UpdateColumnConfig` has no Active-state guard. **Suggested fix:** describe state requirements per operation and explicitly retain Draft column editing.

3. **MEDIUM — [docs/conventions/ef-core.md:7](D:/src/reflekt-F40-ArchitectureDocs/docs/conventions/ef-core.md:7)** — Not every `CardRepository` read includes evidence links. `GetUnreviewedActionItemsBySprintIdAsync` and `GetActionItemsReviewedInSprintAsync` return cards without that include. **Suggested fix:** identify which reads load links and explain that previous-action queries omit them.

4. **MEDIUM — [docs/architecture/architecture.md:334](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:334)** — The JSON error-body guarantee is too broad. Cancellation only sets status 499; cookie authentication’s 401/403 callbacks also set status without writing this body. **Suggested fix:** distinguish serialized exception/validation errors from status-only responses.

5. **MEDIUM — [docs/architecture/architecture.md:293](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:293)** — A disconnect does not invariably send `ReadinessChanged` or clear readiness. `ReadinessTracker.Disconnect` returns no change when another connection remains or the participant was not ready. **Suggested fix:** document these conditions separately from `ParticipantLeft`.

6. **LOW — [docs/architecture/architecture.md:358](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:358)** — There are **13 Pinia stores**, plus `pendingValues.ts`, a utility without `defineStore`. Counting directory files produced the incorrect total of 14. **Suggested fix:** separate the helper from the store inventory and correct the implementation record’s corresponding counts (AC5).

7. **LOW — [docs/architecture/architecture.md:322](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:322)** — The eight repositories do not each wrap `SaveChangesAsync`. `PhaseActionRepository` is a standalone read-only class exposing `GetAllAsync`. **Suggested fix:** describe seven aggregate repositories plus the phase-action reader.

8. **LOW — [docs/architecture/architecture.md:316](D:/src/reflekt-F40-ArchitectureDocs/docs/architecture/architecture.md:316)** — Removed participants are not rechecked by “every method”: `SprintHub.LeaveSprint` performs cleanup without an active-participant check. **Suggested fix:** name the guarded join, readiness and timer operations, and qualify the cleanup exception.

9. **LOW — [docs/specs/F40-ArchitectureDocs/delivery/implementation.md:500](D:/src/reflekt-F40-ArchitectureDocs/docs/specs/F40-ArchitectureDocs/delivery/implementation.md:500)** — AC3 is reported as passing despite its review half remaining unmet. Examples such as `csharp.md:43`’s tuple and `vue.md:8,14`’s generic snippets name no Reflekt type. AC3 states no syntax-example exemption. **Suggested fix:** use Reflekt examples and update the acceptance record.

Every Carry-Over Findings row was checked:

| Row | Assessment |
|---|---|
| One team per creator | **Confirmed:** product documentation says one; configured default is 10. Retain as a separate product-documentation follow-up. |
| Store naming versus skill | **Confirmed:** filenames omit `Store`. Its omission from Accepted deviations is permitted by the stated F39-record scope. |
| Generic language idioms | **Confirmed; AC3-pass interpretation refuted.** Finding 9 applies. |
| New graphify outputs | **Confirmed; acceptable:** the three files are generated output within AC7’s allowed directory. |
| Backlog AC2/AC8 owed at merge | **Confirmed:** Note 1 supplies the required annotation. Apply through the shared-file merge process. |
| Agent files still reference v1 | **Confirmed:** accurately disclosed; agent edits remain outside F40’s scope. |
| Backlog resume instructions stale | **Confirmed:** outside the specified backlog annotation; retain for a later shared-file change note. |

AC1, AC4, AC6 and AC7 pass. AC2/AC8 retain their documented backlog merge obligations. The recorded complete run `mvlc0qqy-5a8ffce8` passed. This review modified no files.

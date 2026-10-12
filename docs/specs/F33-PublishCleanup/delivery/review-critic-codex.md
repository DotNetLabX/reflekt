# F33 — Plan review (Codex, round 1)

**Model:** codex
Primary: Codex — NO-GO, persisted by the architect main session, 2026-10-13. Absolute worktree paths in link targets were made repo-relative on persisting. Light lane: no lowered effort passed (repo sets no codex.effort; the user Codex config decides).

## Verdict: NO-GO

- **HIGH — [plan.md:57](docs/specs/F33-PublishCleanup/delivery/plan.md:57), line 97:** The required empty `src/` diff contradicts step 2’s deletion of `src/.../data/reflekt.db` and `src/.../deploy.zip`. Allow exactly those deletions while prohibiting application source changes.

- **HIGH — [ai-coding/plans/Auth/done.md:36](ai-coding/plans/Auth/done.md:36), [Auth/plan.md:233](ai-coding/plans/Auth/plan.md:233):** Both contain the owner’s personal email. Neither is named for cleanup, and none of the acceptance greps detects emails. Add these specific replacements and an email scan.

- **HIGH — [plan.md:71](docs/specs/F33-PublishCleanup/delivery/plan.md:71):** Step 3’s zero-match requirement becomes impossible once this currently untracked plan is committed. Its own lines 16, 68–70 and 82 contain the searched employer/private identifiers and machine paths. Sanitize evidence prose and define narrow allowances for verification-pattern literals.

- **HIGH — [plan.md:57](docs/specs/F33-PublishCleanup/delivery/plan.md:57), line 98:** Both advertised verification invocations omit the required `--slug`. The installed Nexus runner rejects `--role full` and `--profile complete` with exit 2 before running tests. Supply `--slug F33-PublishCleanup` and explicitly target the worktree.

- **MEDIUM — [plan.md:64](docs/specs/F33-PublishCleanup/delivery/plan.md:64):** The multi-path `git check-ignore -q` command exits **128**: “--quiet is only valid with a single pathname.” Check each pathname separately. Also extend artifact checks to `.db-shm`/`.db-wal` and ignore checks to `publish/` and the listed agent markers; current checks do not prove those requirements.

- **MEDIUM — [plan.md:68](docs/specs/F33-PublishCleanup/delivery/plan.md:68), line 70:** The claimed grep inventory omits two matching sites: [F31 spec.md:13](docs/specs/F31-DarkLightTheme/definition/spec.md:13) contains the private app name, and [ExceptionInfrastructure/plan.md:5](docs/plans/ExceptionInfrastructure/plan.md:5) contains the absolute reference-project path. Include both in the named edits.

- **MEDIUM — [plan.md:76](docs/specs/F33-PublishCleanup/delivery/plan.md:76):** The `client/` replacement list misses [project-rules.md:10](docs/conventions/project-rules.md:10), lines 11–12. These define frontend source-file boundaries. Editing only lines 17, 77 and 78 leaves incorrect boundaries and fails the acceptance grep.

- **MEDIUM — [plan.md:55](docs/specs/F33-PublishCleanup/delivery/plan.md:55):** The team-creation success response shape did **not** change: `CreateTeamResponse(int Id, string Name, DateTime CreatedAt)` is identical at `82fb833` and now. The fixture lacks `NameIdentifier`, which [ClaimsPrincipalExtensions.cs:9](src/Services/Reflekt/Reflekt.API/Auth/ClaimsPrincipalExtensions.cs:9) requires; creation also requires a persisted owner through the foreign key. Correct the diagnosis and fixture requirements, preserving successful-response assertions.

- **MEDIUM — [plan.md:105](docs/specs/F33-PublishCleanup/delivery/plan.md:105):** Verification covers only .NET projects. Neither the solution nor API project invokes the frontend build, so the declared green build can leave Vue/TypeScript broken. Add the existing `npm run build` command from `web/reflekt-web/`.

- **MEDIUM — [plan.md:83](docs/specs/F33-PublishCleanup/delivery/plan.md:83):** Mandatory-parameter parsing and the two greps do not prove deployment parameterization. An unused mandatory `AppName` parameter and retained `az ... --name reflekt` would pass once the resource-group and URL literals were replaced. Inspect every deployment argument and URL construction against the parameters.

- **MEDIUM — [Directory.Packages.props:12](Directory.Packages.props:12), line 33:** Dependency security hygiene has no disposition in the release plan. AngleSharp 1.2.0 falls within its [maintainer advisory](https://github.com/AngleSharp/AngleSharp/security/advisories/GHSA-pgww-w46g-26qg); the committed deployment dependency manifest records SQLitePCLRaw.lib.e_sqlite3 2.1.10, covered by a [high-severity advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q). Add dependency auditing and triage before release. Application exploitability was not established.

- **LOW — [plan.md:88](docs/specs/F33-PublishCleanup/delivery/plan.md:88):** README keyword checks do not establish the promised runnable setup, OAuth/user-secrets instructions, `Seed` configuration, license line, or complete artifact-generation guide. Add explicit content review against those requirements.

Read-only checks confirmed the baseline tag, archive credentials, database contents, 16 tracked OMC files, and test declaration counts. Acceptance greps, deployment parsing and rejecting runner preflights were run. Builds/tests were not rerun; no files were modified.

# F37-DependencyUpgrades — Second Reader Review

## Verdict: COMMENT

**Covers:** the first review, risky Steps 1, 2 and 3 only (light lane; Codex is primary for the whole change).
**Model:** claude-sonnet-5-5

## Pre-commitment Predictions
- A version left behind in the family (one of seven still on 9.x): not found; the diff shows all seven at 10.0.13 and `grep '9\.0\.' Directory.Packages.props` is empty.
- A stray per-project `Version=` bypassing central management: not found (no `Version="` in any csproj under src/tests).
- AngleSharp 1.8.4 still flagged by `--vulnerable`: not found.
- SQLite native library still vulnerable after the EF 10 move: not found, and no pin was needed.
- Net10 TFM mismatch: not found; `Directory.Build.props` already targets net10.0.

## Findings

None at or above the 80 cutoff.

### Plan conformance (Steps 1-3)
- Step 1: `AngleSharp` 1.2.0 to 1.8.4 matches the Accept. The implementation.md records the page, both runs and all four fields identical. The page is a real public page, which the Accept allows.
- Step 2: the seven Microsoft.* entries sit at one shared patch, 10.0.13, with the lookup command and UTC time recorded. `Microsoft.NET.Test.Sdk` stays at 17.14.1 and Scalar is untouched. No forced fix and no source file changed, so there is no migration file and `graphify` was correctly not run.
- Step 3: `dotnet list ... --vulnerable --include-transitive`, re-run by me at review time, reports no vulnerable packages for all nine projects. `SQLitePCLRaw.lib.e_sqlite3` resolved 2.1.13, so no pin was owed under the Accept's conditional.
- Step 1's bump is part of the same `Directory.Packages.props` diff, which has only the 8 intended lines.

## Carry-Over Findings
- **Flaky host start in `CardEndpointTests.InitializeAsync` (medium): confirmed as recorded, not refuted, not a blocker.** The red is a single test failure at `WebApplicationFactory.StartServer()`. Both full-run records carry the same tree hash and the re-run passed. The cause is not established, because the exception message was elided. It is handled as flaky under `questions.md` Q3, and the tripwire stands: a recurrence goes to a solo. I cannot say whether it predates the bump, since 9.0.4 was not re-run. That is an Open Question, not a finding.
- **npm fix owed at merge (low): confirmed.** Step 4 is a change note, and the shared npm files are untouched by design (Q1). I read this note only as it affects the risky steps, so Step 4 itself is outside my scope.

## Positive Observations
- The change is minimal and fully central: eight lines in one file.
- Step 3 followed the conditional-pin Accept and did not add an unneeded transitive pin.
- The implementation.md separates the diagnostic re-run from the red and says plainly that it was not a way round the red.

## Gaps
- The Step 2 flaky exception message was never captured, so the flaky-vs-regression call rests on one red and one green on an identical tree. This is acceptable under Q3, and the tripwire covers it.
- The final complete run is in flight. Step 2's green record `mvkgpdv7-eb5d9956` and the Step 3 fast record are all I could cite.

## Open Questions
- Does `CreateCard_ActionItem_Returns201` host-start flake also occur on 9.0.4? Confidence the bump caused it is below 50, so I raise it for the coordinator and make no finding of it. If the in-flight complete run shows the same failure, take the Q3 tripwire path.

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Run record | pending, complete run in flight (light lane) | the brief's `Run record:` line | not run by me, per brief; Step 2 full run `mvkgpdv7-eb5d9956` pass, as recorded in implementation.md |
| Version lines | pass | `git diff f9832b3 -- Directory.Packages.props` | 8 lines changed; 7 Microsoft.* at 10.0.13; AngleSharp 1.8.4; no 9.x left |
| Vulnerable scan | pass | `dotnet list Reflekt.slnx package --vulnerable --include-transitive` | no vulnerable packages in all 9 projects |
| Stray versions / TFM | pass | grep `Version="` in csproj; `Directory.Build.props` | none stray; `net10.0` |
| Scratch folder | pass | `ls out` | absent |

*Status: COMPLETE — reviewer (second reader), 2026-10-23*

Review: docs/specs/F37-DependencyUpgrades/delivery/review-second.md

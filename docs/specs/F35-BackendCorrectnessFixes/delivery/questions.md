# F35 — Backend Correctness & Security Fixes — Questions

## Q1: New tests for the fixes
**From:** architect
**To:** user
**Status:** Answered
**Step:** Phase 1 analysis

**Context:** This run's instructions say to write no new tests and to change an existing test only where a fix changes what it asserts.
The backlog entry you confirmed for these six fixes says existing test classes may be extended with a regression case where a fix changes behaviour, and this fix's spec relies on that: most of its checks are new test cases, plus three small new test classes for the sign-in redirect rule, the link-fetch address check and the hiding of other people's cards.
The two rules cannot both hold, so the plan cannot be written until one wins.

**Question:** For the security and data fixes in this feature, should the plan add regression test cases?

**Recommendation:** Add regression cases, as the spec says (Recommended) — Each fix gets a test case in the existing test projects, with three new test classes where no class covers the area; no new test project, framework or package.
Add regression cases to existing test classes only — The same fixes are tested, but the three areas with no class get their cases placed in the nearest existing class instead.
No new test cases at all — Only the existing tests whose assertions the fixes change are updated; the open-redirect, link-fetch and card-hiding fixes ship with no test, checked only by code search and one recorded manual run.
**Confidence:** medium — the confirmed backlog wording and the spec agree on regression cases, but this run's instructions are your most recent words and say the opposite.

### Answer
**Answered by:** the owner's standing campaign directive, relayed by the campaign's runner (binds as an owner answer) — not a presumed default.
**Answer:** No new test cases at all. The directive reads: "Write no new tests; update an existing test only where a fix changes what it asserts." Only existing tests whose assertions a fix changes are updated (e.g. the `SprintTests` throw-type assertions). The fixes whose spec checks were new test cases are verified by code trace in the done check and code review, and by recorded manual/evidence runs where the spec names one. Where a spec acceptance line asks for a new test, the plan rewrites it as a code-trace or existing-test check and lists each such change in `## Decisions`.
**Date:** 2026-10-23

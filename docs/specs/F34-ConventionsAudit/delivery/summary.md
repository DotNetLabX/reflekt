# F34 — Conventions & Architecture Audit — Summary

Mode: architect-led fast lane (light) — audit only, no developer build

## Status: COMPLETE

## What Was Built
- A read-only audit of the code against the repo's own conventions and skills, and of the architecture docs against the code, run as five parallel auditors. Result: `audit-report.md` (themes, proposed fix features, owner decisions) with the full finding tables in `findings/` (~180 rows, ~140 distinct).
- Six proposed fix features (F35–F40) added to `docs/backlog.md` as proposals; IDs wait for the owner.

## Key Outcomes
- No source, test or config changes; docs only (`docs/specs/F34-ConventionsAudit/**`, `docs/backlog.md`).
- **Full suite:** pass — 53 s (recorded 2026-10-13T00:09:27Z; complete red on a fast-green tree: not measured; filtered: no). Run with the Google credential settings blanked.
- **Map delta:** none
- Review: none — an audit report, not a build; each auditor verified its own rows against the code. No plan review, done check or code review ran (disclosed, not simulated).

## Deviations from Plan
- No `plan.md`: F34's definition is its backlog row (an audit); the method is recorded in `audit-report.md` § How it was run.

## Notes
- **Highest-value findings:** domain events can be lost (singleton interceptor); Writing-phase card text leaks over SignalR; domain-rule violations return 500 with raw messages; rounded corners dropped app-wide; the architecture doc is still the v1 proposal; the documented login allowlist is not enforced.
- **Owner decisions listed in the report:** confirm F35–F40; allowlist (code or docs); rewrite root `CLAUDE.md` or keep it as evidence; Microsoft.* 10.0.x bump; campaign vs one at a time; the history scrub and publish.
- **Under the bar:** none — **Carry-over row:** none (no MEDIUM)
- **Merge:** not done — owner away; committed on branch `F34-ConventionsAudit`, stacked on `F33-PublishCleanup`.

# F34 Conventions & Architecture Audit — Lessons

## Architect Lessons

- **Split an audit by the rule source, not by folder.** Five auditors keyed to one rule set each (written conventions, pattern skills, frontend rules, architecture docs, deps/tests/conventions index) produced overlapping rows on the same defects (e.g. the singleton interceptor surfaced twice) — that overlap is cross-validation, and the merge dedupes it cheaply. **Evidence:** [F34-ConventionsAudit]
- **A conventions audit finds real defects, not only style.** About a fifth of the rows were behaviour bugs (lost domain events, an anonymity leak, 500s, silent chime). Group those into their own fix feature ahead of the conformance refactors, so the risky refactors run on a corrected baseline. **Evidence:** [F34-ConventionsAudit]
- **Audit notes copied into the repo need the same scrub as delivery artifacts.** Every auditor wrote the absolute worktree path in its header; the privacy greps caught it before commit. **Evidence:** [F34-ConventionsAudit]
- **Proposed feature IDs are proposals until the owner confirms them.** With the owner away, the audit ends at the report and proposed backlog rows; planning waits. **Evidence:** [F34-ConventionsAudit]

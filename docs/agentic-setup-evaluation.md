# Agentic Setup Evaluation

**Date:** 2026-05-25
**Evaluators:** OMC Critic (thoroughness, coherence, conventions) + OMC Architect (system design, strategy, anti-patterns)
**Scope:** `.claude/agents/`, `.claude/rules/`, `.claude/conventions/`, `.claude/skills/`, `CLAUDE.md`, `docs/backlog.md`
**System size:** 8 agents, 10 rules, 7 conventions, 37 skills, 62 files total

---

## Executive Summary

**Combined Score: 8.25 / 10** (Critic: 8.4, Architect: 8.1)

**Verdict: ACCEPT-WITH-RESERVATIONS**

This is an exceptionally well-evolved agentic system refined through 32+ completed features. The pipeline flow is clear, role boundaries are sharp, artifact formats are consistent, and the closed-loop learning pipeline (lessons -> learner -> promotion -> improved instructions) is genuinely self-improving. The reservations center on two critical broken references, unbounded growth without compression mechanisms, and a phantom KB infrastructure that was never created.

**Root tension:** Thoroughness vs. context efficiency. Every rule exists because a real failure occurred — but the additive nature of lesson promotion means the system only grows, never shrinks.

---

## Dimension Scores

| # | Dimension | Critic | Architect | Avg | Key Strength | Key Weakness |
|---|-----------|--------|-----------|-----|-------------|--------------|
| 1 | Completeness / System Design | 8 | 9 | 8.5 | Two-phase invocation; hub-and-spoke messaging | Phantom KB infrastructure; no silent-degradation detection |
| 2 | Coherence / Separation of Concerns | 9 | 9 | 9.0 | Agents align remarkably well; explicit "Never Do" sections | Critic sub-agent spawning bypasses hub-and-spoke (intentional but undocumented) |
| 3 | Role Boundaries | 10 | — | 10.0 | Every agent has crisp boundaries; PO can't read code, architect can't write code | None detected |
| 4 | Pipeline Flow | 9 | — | 9.0 | Two-phase checkpoints; resume protocol | No concurrent pipeline handling; no mid-implementation rollback |
| 5 | Skill Coverage | 9 | 8 | 8.5 | 37 skills matching tech stack; improve-skills quality gate | No skill index beyond full frontmatter scan; no SignalR hub skill |
| 6 | Guardrails | 8 | — | 8.0 | Concrete, verifiable rules; meta-guardrail enforcement | Duplication across 3 files; "No secrets" only in project-rules.md |
| 7 | Scalability / Extensibility | 7 | 8 | 7.5 | Clear agent addition path; skill scaffolding | Serial pipeline; growing context costs; no concurrent pipelines |
| 8 | DRY-ness / Context Efficiency | 7 | 7 | 7.0 | On-demand skills; KB as source-file proxy | Guardrail duplication; growing agent file sizes; solo tax |
| 9 | Onboarding / Information Architecture | 9 | 8 | 8.5 | README decision tree; five-layer loading strategy | No file dependency diagram; agents-workflow.md taxes solo sessions |
| 10 | Convention Consistency / Anti-patterns | 8 | 8 | 8.0 | Pre-commitment predictions; explicit scope guards | Instruction creep; no compression mechanism |
| 11 | Error Handling & Resilience | — | 7 | 7.0 | Circuit breakers; timeout recovery; idempotency gates | No intermediate rollback; skill fallback is fragile |
| 12 | Feedback Loops | — | 9 | 9.0 | Learner with recurrence tracking; known-deviations table | Promotion threshold slow for single-developer projects |
| 13 | Authority Model | — | 9 | 9.0 | Explicit escalation chains; user-decision guardrails | Three-way skill/plan/codebase priority could be more concise |
| 14 | Artifact Lifecycle | — | 7 | 7.0 | summary.md as completion signal; resume via comm log | No pruning policy; no stale-reference lint |

---

## Critical Findings (blocks execution)

### C1. Broken `@docs/architecture/index.md` reference — 4 agents affected

**Files:** `architect.md:25`, `developer.md:17`, `reviewer.md:19`, `solo.md:15`
**Impact:** These agents start every session without architecture context. The actual file is `docs/architecture/v1.md`.
**Fix:** Update all 4 agent files to `@docs/architecture/v1.md`, or create `docs/architecture/index.md` as a redirect.

### C2. `docs/kb/` directory does not exist — KB rules reference a phantom system

**Files:** `.claude/rules/kb-navigation.md`, `.claude/rules/kb-maintenance.md`
**Impact:** Two auto-loaded rules (consuming context every session) instruct agents to use a KB system that was never created. Developer's implementation.md has a "KB Changes" section that can never be filled. Plan template has a "KB Impact" section that always says "None".
**Fix:** Either create `docs/kb/index.md` with skeleton entries for core domain concepts (sprint, team, card, participant), or remove both KB rules from `.claude/rules/` until the KB is populated.

---

## Major Findings (causes significant rework)

### M1. Guardrails duplicated across 3 files with no single source of truth

**Files:** `CLAUDE.md:144-151`, `docs/conventions/project-rules.md:22-28`, partial duplication in agent files
**Impact:** The "No secrets" guardrail exists only in `project-rules.md`, not `CLAUDE.md`. Guardrail changes require 2-3 file updates. Non-pipeline sessions that only read `CLAUDE.md` miss the "No secrets" rule.
**Fix:** Designate `CLAUDE.md` as canonical guardrail source; have `project-rules.md` reference it. Or move all guardrails to a dedicated `.claude/rules/guardrails.md` (auto-loaded).

### M2. `coding-conventions.md` is a 3-line redirect with no inline content

**Files:** `docs/conventions/coding-conventions.md` (loaded by 3 agents)
**Impact:** Contains only `@csharp.md`, `@vue.md`, `@ef-core.md`. If nested `@` directives are not recursively resolved, agents get 3 lines instead of actual conventions.
**Fix:** Verify nested `@` resolution behavior. If not supported, inline the content or have agents reference sub-files directly.

### M3. No intermediate rollback mechanism for large implementations

**Both evaluators flagged this.**
**Impact:** The 2-commit strategy (plan + final) means if step 7 of 10 corrupts the codebase, there is no commit anchor to revert to.
**Fix:** For plans with 8+ steps, add a "checkpoint commit" after every 4 steps. Keep the 2-commit default for smaller features.

### M4. Resume flow doesn't handle detached HEAD or rebased branches

**File:** `team-lead.md:264-269`
**Impact:** A developer who rebases before resuming will be blocked with no recovery path.
**Fix:** Add handling for edge cases and provide user options (switch branch, update log, start fresh).

---

## Minor Findings

| # | Finding | Location | Fix |
|---|---------|----------|-----|
| 1 | Critic sub-agent spawned directly by architect, bypassing hub-and-spoke | `architect.md:147` | Add explicit carve-out in `agents-workflow.md` for sub-agent spawning |
| 2 | `docs/skill-backlog.md` referenced by `improve-skills` but doesn't exist | `improve-skills/SKILL.md` | Self-healing (create-on-demand), but indicates learner pipeline hasn't run |
| 3 | `create-feature` skill has Carter/MinAPI paths — dead code for this project | `create-feature/SKILL.md` | Not harmful but adds cognitive load |
| 4 | README skill count says 36, actual is 37 | `.claude/README.md:34` | Update count or remove it (will always drift) |
| 5 | Step numbering error in `solo.md` — two items numbered "4." | `solo.md:113-115` | Fix numbering |
| 6 | No monitoring/observability for pipeline health across features | System-wide | No structured way to measure avg cycle count, common failure points, duration |
| 7 | No guidance for concurrent pipeline runs | System-wide | Branch management and agent ID tracking assume single active pipeline |
| 8 | `agents-workflow.md` (154 lines) auto-loaded even for solo sessions that use none of it | `.claude/rules/agents-workflow.md` | README acknowledges: "Solo sessions pay a small tax" |

---

## Architectural Recommendations

### R1. Add a context budget per agent role (Low effort, High impact)

Add to `.claude/README.md` a max lines table: solo < 800, developer < 1,200, architect < 1,500. When the learner promotes a lesson exceeding the budget, it must also identify a line to remove or compress. This creates the missing "shrink" pressure against unbounded growth.

### R2. Add a skill index mechanism (Medium effort, High impact)

At 37 skills, scanning all frontmatter is ~200+ lines per planning session. Add a generated `skills-index.md` maintained automatically by the `improve-skills` skill on every create/fix/delete. This is a build artifact, not a human-maintained duplicate — drift risk is eliminated because the learner is the only writer.

### R3. Consolidate small auto-loaded rules (Low effort, Medium impact)

Merge `spec-authority.md` (3 lines), `agent-invocation.md` (5 lines), `research-before-asking.md` (7 lines) into a single `behavioral-rules.md`. Move the Artifact Formats table from `agents-workflow.md` to a `@`-loaded convention file. Saves ~15 lines auto-load, reduces rule files from 10 to 8.

### R4. Add artifact pruning mechanism (Medium effort, High impact)

Create `.claude/rules/artifact-lifecycle.md` defining: (a) when delivery artifacts can be archived (e.g., 3 features after completion), (b) lint check for stale plan-to-spec references, (c) Questions.md cleanup marking all questions Resolved at pipeline completion. The learner should trigger this during lessons processing.

### R5. Define conflicting-output resolution (Low effort, Medium impact)

Add to `pipeline-protocol.md`: reviewer findings take precedence for code-level conformance (they read the code); architect findings take precedence for architectural conformance.

### R6. Add intermediate commit anchors for large features (Low effort, Medium impact)

Extend commit protocol: for plans with 8+ steps, commit after every 4 completed steps. Provides rollback anchors without changing the 2-commit default for smaller features.

---

## What's Working Exceptionally Well

1. **Two-phase agent invocation** — turning a platform limitation (agents can't pause) into a quality gate is architecturally elegant
2. **Closed-loop learning** — lessons -> learner -> promotion is genuinely self-improving (F30 skill gap was subsequently fixed)
3. **Known deviations table** — tracking recurring mistakes with wrong/correct patterns and frequency counts is a powerful pattern
4. **Pre-commitment prediction** — critic and reviewer predict problems before reading, countering confirmation bias
5. **Role boundaries** — every agent has a "What You Never Do" section; the 10/10 score from the critic is earned
6. **Communication log as state machine** — Branch, Step, Cycle, Agent IDs, Plan Steps enable cross-session resume
7. **Five-layer loading strategy** — rules (auto) -> conventions (@) -> agents (on invoke) -> skills (on demand) -> stack docs (@) is well-designed

---

## Open Questions

1. Does the Claude Code `@` directive recursively resolve nested `@` references? (Affects M2)
2. How does `@` handle a missing file — silent skip, warn, or error? (Affects C1 severity)
3. The `service-registration` and `domain-patterns` skills reference BuildingBlocks (`Blocks.MediatR`, `Blocks.FastEndpoints`) — do these exist in the current repo?
4. What is the actual context window cost of the current auto-load (~474 lines before any agent loads)? Is there telemetry?

---

## Priority Fix Order

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| 1 | C1: Fix broken `@docs/architecture/index.md` in 4 agents | 5 min | Critical — every pipeline run |
| 2 | C2: Create KB skeleton or remove phantom rules | 30 min | Critical — dead context every session |
| 3 | M1: Consolidate guardrails to single source | 15 min | High — maintenance risk |
| 4 | R1: Add context budget table to README | 10 min | High — prevents unbounded growth |
| 5 | M3/R6: Add checkpoint commits for large features | 10 min | Medium — rollback safety |
| 6 | R2: Generate skill index | 30 min | Medium — planning efficiency |
| 7 | R3: Consolidate small rules | 15 min | Medium — context savings |
| 8 | Minor fixes (numbering, counts, stale refs) | 10 min | Low — polish |

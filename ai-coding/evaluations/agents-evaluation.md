# Agent Evaluation — Reflekt Project

**Date:** 2026-05-02
**Evaluated by:** Architect (Opus)
**Stack context:** .NET 10, DDD, CQRS, Vertical Slice, FastEndpoints, EF Core, SignalR, MassTransit

## Scoring Criteria

| Criteria | What it measures |
|---|---|
| **Role clarity** | Non-overlapping responsibility, clear boundaries |
| **Stack self-containment** | Does the agent body carry .NET/DDD/FastEndpoints/EF Core conventions without relying on CLAUDE.md? |
| **Tool scoping** | Does it restrict tools to what the role needs? |
| **Description quality** | Will Claude route to it reliably? (trigger-sentence pattern) |
| **Pipeline integration** | Does it coordinate via file handoffs correctly? |
| **Output format** | Are outputs structured and predictable? |

## Critical Finding

**Subagents do NOT inherit CLAUDE.md.** All guardrails, domain event conventions, repo structure, tech stack rules — none of that is available to agents when they run as subagents via Agent Teams. Every agent in this project relies on CLAUDE.md being in scope, but it isn't.

---

## 1. Planner — Score: 8/10

**Strong:**
- Best role definition in the set. Intent classification (trivial/scoped/complex/refactoring) right-sizes effort — most community agents don't do this.
- Auto-approve gate (≤12 steps) reduces human bottleneck.
- Pre-commitment predictions before done check combat rubber-stamping.
- Gap analysis step before writing is rare and valuable.
- Pipeline integration is complete (done check, question handling, escalation, summary writing).

**Weak:**
- No `tools` field — should restrict to read-only (Read, Glob, Grep, Write for plan artifacts only). Currently inherits everything, meaning the planner could modify source code.
- Description is good but not trigger-sentence format. Should be: _"Invoked when a feature needs planning, a done check is needed, or executor has questions. Use for feature design, implementation planning, Step 1 review."_
- `@docs/architecture/v1.md` won't auto-load for subagents — the architecture knowledge it depends on isn't available.
- No stack conventions in body. It says "follow existing patterns" but doesn't carry the guardrails (no service layer, no repo interfaces, domain events carry aggregate references, etc.). When running as a subagent, it plans blind.

**To reach 10:** Add a condensed stack conventions section to the body. Scope tools. Fix description to trigger-sentence format.

---

## 2. Executor — Score: 7.5/10

**Strong:**
- Task classification (trivial/scoped/complex) mirrors planner's — good consistency.
- Circuit breaker (3 failed attempts → stop and ask) is excellent for .NET builds where errors can cascade.
- Completion checklist with grep for debug artifacts is practical.
- "When There's No Plan" section handles edge cases cleanly.
- Lessons + skill gap logging is mature.

**Weak:**
- Three `@` directives that won't load as a subagent: `architecture/v1.md`, `coding-standards.md`, `tech-stack.md`. The executor has zero stack knowledge when running independently.
- No inline stack conventions. It doesn't know FastEndpoints endpoint structure, aggregate patterns, EF Core migration commands, or the project reference graph. It relies entirely on "find existing examples" which works for pattern-matching but fails for greenfield additions (new service, first feature in a module).
- Description is functional but not trigger-sentence format.

**To reach 10:** Inline a condensed tech stack + coding standards section. The executor is the agent that writes code — it needs the conventions most.

---

## 3. Reviewer — Score: 8.5/10

**Strong:**
- Best-structured agent in the set. Two-stage review (conformance then quality) prevents style-nitpick distraction.
- Pre-commitment predictions are a genuine anti-bias technique — rare in community agents.
- Self-audit step (confidence rating, could-executor-refute, flaw-vs-preference) is sophisticated.
- Severity ratings with clear approval criteria are exactly what the community recommends.
- Fresh evidence requirement ("no approval without fresh build output") prevents rubber-stamping.
- Codex cross-validation is a unique differentiator.
- Positive observations section reinforces good patterns — most review agents only criticize.
- "Write the file first, then message" prevents lost reviews.

**Weak:**
- No `tools` field — reviewer should be explicitly read-only + Write (for review.md only). Currently could accidentally modify source code.
- Same `@` dependency issue — no stack knowledge when running as subagent.
- Description could be sharper: _"Invoked after planner's Step 1 done check passes. Conducts severity-rated code review against the implementation plan. Never modifies source code."_

**To reach 10:** Scope tools to read-only + Write. Inline the key quality checks specific to .NET (N+1 queries in EF Core, domain rules in aggregates not handlers, etc.).

---

## 4. Team Lead — Score: 7.5/10

**Strong:**
- Clear entry-point role — no overlap with other agents.
- Status check derived mechanically from file existence — no guessing.
- Launch options menu (Fast/Standard/Standard+Codex) is practical and flexible.
- "Ask first, read after" prevents unnecessary file scanning.
- Team shutdown protocol with investigation before closing is mature.
- Collaboration reports requiring agent responses before reporting prevents partial findings.

**Weak:**
- No `tools` field — team lead needs Bash (for codex --version check), Read, Glob, and team tools. Doesn't need Write or Edit on source files.
- Description is functional but generic.
- Codex availability check is brittle — `codex --version` via Bash could fail for many reasons.
- Numbering error in the agent file: step 4 appears twice. Minor but sloppy.

**To reach 10:** Fix the duplicate numbering. Scope tools. The team lead is well-designed for its role — it's the least stack-dependent agent because it only coordinates.

---

## 5. PO (Product Owner) — Score: 9/10

**Strong:**
- Best agent in the set for its specific role. Interview-mode-by-default is the right call for feature shaping.
- "Research first, then ask in one batch" prevents the painful 10-round drip-feed.
- Challenge patterns ("What if we didn't do X — what breaks?") are concrete and actionable.
- Gap check before writing (Complete? Testable? Unambiguous? Edge cases? Guardrails?) is thorough.
- Cross-check against source spec prevents spec drift.
- External research with cross-check against internal specs is balanced — doesn't blindly adopt.
- "Never ask about codebase facts" is excellent — the PO looks things up itself.

**Weak:**
- No `tools` field — PO should have read-only + Write (for specs only) + WebSearch.
- `@docs/specs/v1.md` and `@docs/backlog.md` won't auto-load.
- Stack awareness isn't needed for PO (it works in product language), so the self-containment gap matters less here.

**To reach 10:** Scope tools. It's the closest to perfect because the PO role is genuinely stack-independent — it works in domain language, not code language.

---

## 6. Solo — Score: 6.5/10

**Strong:**
- Discuss-then-implement flow is right for small changes.
- Scope guard ("This is getting bigger than solo scope") is a useful safety valve.
- Optional `/review` integration at completion is practical.
- Artifact table (what it writes vs. what it doesn't) is clear.

**Weak:**
- Thinnest agent in the set. It's essentially "executor-lite + discuss first" but without the executor's circuit breaker, task classification, or skill gap logging.
- No stack conventions — worse than the executor because the solo agent is the one most likely to work without a plan. It needs to carry conventions to make correct decisions independently.
- No `tools` field.
- No `@` references to any conventions (except the three that won't load anyway).
- Description is adequate but not trigger-sentence.
- Missing: how to handle build failures, what to do when patterns conflict, when to check skills.

**To reach 10:** This agent needs the most work. Either beef it up with inline conventions and the executor's safety features, or consider whether "solo" should just be a mode flag on the executor rather than a separate agent.

---

## 7. Codebase Scanner — Score: 5/10

**Strong:**
- Thorough scan checklist covering 16 categories.
- "How to work" section is practical (Glob → Grep → Read → extract real examples).
- Output format is well-defined.

**Weak:**
- Outdated. References "Articles Application" (old project name), Carter endpoints, Minimal APIs, MediatR, JWT auth, multi-tenancy — most of which may no longer be current in Reflekt.
- `model: sonnet` — scanning 16 categories across the entire codebase is a heavy task. Sonnet may struggle with the volume.
- No `tools` field (should be read-only).
- Description is generic.
- This is a one-shot utility, not a pipeline participant. It doesn't integrate with any other agent. Could be a skill instead.
- The output path `.claude/docs/architecture-reference.md` may conflict with the graphify knowledge graph that already serves a similar purpose.

**To reach 10:** Update for Reflekt (not "Articles Application"). Consider making this a skill rather than an agent. Reconcile with graphify — either this replaces the graph report or it feeds into it, but both existing simultaneously is confusing.

---

## Summary Scorecard

| Agent | Score | Top Strength | Top Weakness |
|---|---|---|---|
| **PO** | 9 | Interview-mode + challenge patterns | Description not trigger-sentence |
| **Reviewer** | 8.5 | Two-stage review + self-audit + fresh evidence | No inline stack checks |
| **Planner** | 8 | Intent classification + auto-approve gate | No stack conventions in body |
| **Team Lead** | 7.5 | Mechanical status check + launch options | Duplicate numbering |
| **Executor** | 7.5 | Circuit breaker + completion checklist | No inline conventions, description not trigger-sentence |
| **Solo** | 6.5 | Scope guard | Too thin — missing safety features, wrong model (should be opusplan) |
| **Codebase Scanner** | 5 | Thorough checklist | Outdated, overlaps with graphify |

---

## Cross-Cutting Issues

| Issue | Impact | Fix |
|---|---|---|
| **No agent self-contains stack conventions** | When running as subagents, they have zero knowledge of .NET/DDD/FastEndpoints/EF Core guardrails | Inline stack rules into executor, reviewer, solo (full) and planner (light) |
| **`@` directives don't work for subagents** | Architecture docs, coding standards, tech stack — none load | Addressed by inlining the critical rules |
| **Descriptions aren't trigger-sentences** | Routing relies on Claude's interpretation rather than explicit triggers | Rewrite as "Invoked when... Use for... Do not use for..." |
| **Solo uses wrong model** | `claude-opus-4-6` instead of `opusplan` — implementation agent without plan gates | Change to `opusplan` |

---

## Recommendations (priority order)

1. **Fix solo's model** — change from `claude-opus-4-6` to `opusplan` (implementation agents need plan mode).
2. **Inline stack rules** into executor, reviewer, solo (full) and planner (light). ~200 words covering: guardrails, domain event pattern, project reference graph, FastEndpoints structure, EF Core migration commands. Duplicated across 4 files — unavoidable since CC subagents don't support shared includes.
3. **Rewrite descriptions** to trigger-sentence format.
4. **Retire or update codebase scanner** — either update for Reflekt and reconcile with graphify, or convert to a skill.
5. **Decide solo's fate** — either promote it to a full executor-mode or merge it as a flag.

---

## Sources

- [Official Claude Code docs — Sub-agents](https://code.claude.com/docs/en/sub-agents)
- [Official Claude Code docs — Agent Teams](https://code.claude.com/docs/en/agent-teams)
- [Official Claude Code docs — Best Practices](https://code.claude.com/docs/en/best-practices)
- [wshobson/agents — 184-agent community library](https://github.com/wshobson/agents)
- [kevinschawinski/claude-agents — Planner/Executor/Critic pattern](https://github.com/kevinschawinski/claude-agents)
- [stevekinney.com — Sub-agent anti-patterns](https://stevekinney.com/courses/ai-development/subagent-anti-patterns)

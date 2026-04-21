# Multi-Agent Automation — Next Steps

## Context

We evaluated OMC, CC Agent Teams, and custom automation (see `multi-agent-automation-findings.md`). Decision: use CC Agent Teams directly with our own agents. OMC's `team` pipeline is hardcoded (plan → prd → exec → verify → fix) and doesn't match our flow — no questions loop, no two-step review split.

## What we want

Three agents in `.claude/agents/` — planner, executor, reviewer — that self-coordinate via SendMessage on CC Agent Teams. No 4th orchestrator. The pipeline lives in the agent files themselves.

### Why three agents instead of two

The current Architect handles both planning and code review (Step 2). Step 2 is conformance verification — Sonnet-class work, not Opus reasoning. Splitting it out:
- Saves Opus tokens on the most expensive review step
- Enforces "never approve your own output" structurally
- Enables overnight loops (executor → reviewer → fix cycle) without Opus

### Why SendMessage instead of OMC's pipeline

Our flow has bidirectional handoffs that OMC's linear pipeline can't express:
- Executor can write `questions.md` mid-implementation and needs the planner to answer before continuing
- Planner does Step 1 (done check) before handing to reviewer for Step 2 (code review)
- Reviewer routes back to executor (fixes needed) or planner (approved)

### What to cherry-pick from OMC agents

Already evaluated OMC's planner, executor, architect, and code-reviewer. Worth taking:
- **Planner:** "Never ask the user codebase facts — look them up." Intent classification (trivial/refactoring/build/mid-sized) to match plan depth.
- **Executor:** Task classification, 3-failure circuit breaker, smallest viable change principle, grep for debug artifacts before completing.
- **Reviewer:** Severity ratings (CRITICAL/HIGH/MEDIUM/LOW) with clear approval gates. Structured output with file references and fix suggestions.

### What to preserve from current setup

- Plan-as-contract with full file paths, skill references, performance approach
- File-based handoffs (spec → plan → questions → done → review → lessons)
- Selective convention imports per agent
- Two-step review gate (Step 1 on planner, Step 2 on reviewer)
- `agents-workflow.md` as the shared contract including the questions protocol

## Models

- Planner: Opus
- Executor: opusplan
- Reviewer: Sonnet

## Test plan

CC Agent Teams with three teammates on a trivial feature we've already built. Verify the full messaging loop works before using it on real work.

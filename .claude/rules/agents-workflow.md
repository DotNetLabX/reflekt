# Agent Coordination Protocol

Three-agent pipeline for feature development. Agents coordinate through file-based handoffs and SendMessage in CC Agent Teams.

**Deploy to:** `.claude/rules/agents-workflow.md` (auto-loads every session)

## Team Lead Rules

- Do not read files before delegating to agents. Send the file path in the message and let the agent read it.
- Only read a file yourself when you need its content to make a routing or coordination decision — not to relay it.
- Relay user questions about plan content to the planner — do not investigate or answer them yourself.
- When a reusable rule or convention is identified, capture it in the appropriate rule or agent file — not in memory. Memory is for context that doesn't fit in rule files (user preferences, project state, external references).

## Agents

| Agent | Model | Scope |
|-------|-------|-------|
| planner | opus | Specs, plans, Step 1 review (done check), question answers, escalation decisions |
| executor | opusplan | Implementation, implementation.md, questions.md |
| reviewer | sonnet | Step 2 review (code review), severity-rated conformance checks |

## Pipeline

```
Human → planner (spec → plan → human approves)
                                    ↓
                              executor (implement → implementation.md)
                                    ↓
                              planner (Step 1: done check)
                               ↓ fail          ↓ pass
                        executor (fix)    reviewer (Step 2: code review)
                                           ↓ approve       ↓ request changes
                                    planner (close)    executor ↔ reviewer
                                                       (max 3 fix cycles)
                                                            ↓ exhausted
                                                      planner (escalation)
```

## SendMessage Handoffs

Each handoff: trigger → sender → receiver → action.

### Executor → Planner: Ready for review
**Trigger:** Any implementation round complete (initial, amendment, or additional plan steps) — implementation.md written/updated.
**Message:** "implementation.md written for {FeatureName}, ready for Step 1."
**Planner:** Reads implementation.md against plan. Pass → messages reviewer. Fail → writes review.md, messages executor.

### Planner → Reviewer: Step 1 passed
**Trigger:** Done check passes.
**Message:** "Step 1 passed for {FeatureName}. Plan: ai-coding/plans/{FeatureName}/plan.md"
**Reviewer:** Re-reads plan, reviews code, writes review.md, issues verdict.

### Reviewer → Executor: Fixes needed
**Trigger:** CRITICAL or HIGH issues found.
**Message:** "Fixes needed for {FeatureName}, see review.md. Cycle {N}/3."
**Executor:** Reads review.md, fixes each item, updates implementation.md, messages reviewer.

### Executor → Reviewer: Fixes applied
**Trigger:** All review items addressed.
**Message:** "Fixes applied for {FeatureName}, ready for re-review. Cycle {N}/3."
**Reviewer:** Re-reviews, issues new verdict.

### Reviewer → Planner: Approved
**Trigger:** APPROVE verdict.
**Message:** "APPROVED: {FeatureName}."
**Planner:** Writes summary.md, updates lessons.md, reports to human.

### Reviewer → Planner: Escalation
**Trigger:** 3 fix cycles exhausted OR architecture decision needed.
**Message:** "ESCALATION for {FeatureName}: {reason}."
**Planner:** Reads review.md, decides: update plan or redirect executor. Messages executor with resolution.

### Executor → Planner: Question
**Trigger:** Blocker mid-implementation.
**Message:** "Blocked on step {N} for {FeatureName}. Question in questions.md."
**Planner:** Reads questions.md, answers inline, updates plan if needed, messages executor: "Answered, continue from step {N}."

## Cycle Caps

| Loop | Max | Escalation |
|------|-----|------------|
| Reviewer ↔ Executor fix cycle | 3 | → Planner |
| Executor questions (same area) | 3 | → Human |
| Planner escalation resolution | 1 | → Human |

After escalation to human, agents STOP and wait.

## Implementation Plan Format

Plans saved to `ai-coding/plans/{FeatureName}/plan.md` by planner.

```
# {Feature Name}

## Context
What problem this solves. Which service(s) impacted and why.

## Scope
In scope. Explicitly out of scope.

## Domain Model Changes
New/modified aggregates, entities, value objects, domain events.

## Data Model Changes
New tables, columns, relationships, migrations needed.

## Implementation Steps
Numbered steps — each step is one focused task.
For each step:
- What to do (not how to code it)
- Which files to create or modify (full paths)
- What pattern to follow (reference an existing file)
- Which skill to use if one applies
- Dependencies on previous steps

## Cross-Service Changes (if applicable)
gRPC contract changes, integration events, consumers.

## Migration Notes
EF Core migration commands. Seed data if needed.

## Testing Strategy
Key scenarios to test.

## Open Questions
Unresolved decisions needing input.
```

## Implementation File Format

Written by executor after each implementation round: `ai-coding/plans/{FeatureName}/implementation.md`.

```
# {Feature Name} — Implementation

## Files Created
- `full/path/to/File.cs` — what it does

## Files Modified
- `full/path/to/File.cs` — what changed and why

## Key Decisions
- Implementation choices not specified in the plan

## Deviations from Plan
- Steps done differently, with reasons
```

## Summary File Format

Written by planner after reviewer approval: `ai-coding/plans/{FeatureName}/summary.md`. Its existence means the pipeline completed successfully.

```
# {Feature Name} — Summary

## Status: COMPLETE

## What Was Built
- [1-3 sentence description of the feature/change]

## Key Outcomes
- [Files created/modified count]
- [Build status]
- [Review verdict and cycle count]

## Deviations from Plan
- [Any deviations, or "None"]

## Notes
- [Anything the user should know before committing]
```

## Questions File Format

Written by executor when blocked. Planner answers inline.

```
# {Feature Name} — Questions

## Q1: [Short title]
**From:** executor
**To:** planner
**Status:** Open | Answered | Resolved
**Step:** [Plan step number and name]
**File:** [File being worked on, if relevant]

**Context:** [What was being done, what was tried, what is unclear]

**Question:** [Specific question or decision needed]

### Answer
[Planner fills this in, sets Status → Answered]
```

**Rules:**
- One question per section. Multiple blockers = multiple sections.
- Include enough context that planner can answer without reading executor's work.
- If the answer changes the plan, planner updates the plan. Plan stays source of truth.

## Lessons File Format

All three agents append under their own heading: `ai-coding/plans/{FeatureName}/lessons.md`.

```
# {Feature Name} — Lessons

## Planner Lessons
- Plan instructions that were ambiguous
- Architecture decisions needing documentation
- Skill gaps discovered

## Executor Lessons
- Patterns discovered not yet documented
- Inconsistencies found
- Steps missing from a skill

## Reviewer Lessons
- Review criteria that were unclear
- Recurring code quality issues
- Patterns that should become conventions
```

Only add items not already in CLAUDE.md, convention files, skills, or agent files. Update before `/compact` or `/clear`.

## Review Checklist

### Step 1: Done check (planner, no code reading)
- Every plan step has a corresponding implementation.md entry
- No plan steps missing or silently skipped
- Reported deviations have reasons
- No unexpected files or scope creep

### Step 2: Code review (reviewer, reads implementation)
- Each plan instruction has corresponding code that matches
- Referenced patterns and skills were followed
- Naming conventions match coding standards
- Build passes (fresh output, not assumed)
- No new patterns invented
- Domain rules enforced in aggregates, not handlers
- Deviations flagged with verdict: plan wrong or code wrong
- Security: no hardcoded secrets, inputs validated, no injection vectors
- Logic: all branches reachable, no off-by-one, null handling correct
- Performance: no N+1 queries, bulk vs per-entity matches plan

### Severity Ratings (reviewer)
- **CRITICAL**: Security vulnerability, data loss risk, fundamentally wrong approach. Blocks merge.
- **HIGH**: Logic error, missing error handling, plan deviation without justification. Should fix.
- **MEDIUM**: Suboptimal pattern, minor inconsistency. Consider fixing.
- **LOW**: Style preference, minor improvement. Optional.

### Verdict
- **APPROVE**: No CRITICAL or HIGH issues.
- **REQUEST CHANGES**: Any CRITICAL or HIGH issue present.
- **COMMENT**: Only MEDIUM/LOW, no blockers.

## Review Output Format

Reviewer saves to `ai-coding/plans/{FeatureName}/review.md`:

```
# {Feature Name} — Review

## Verdict: APPROVE | REQUEST CHANGES | COMMENT

## Pre-commitment Predictions
- [What you expected to find vs what you found]

## Findings

### [SEVERITY] Finding title
**File:** `path/to/file.cs:line`
**Issue:** What's wrong
**Fix:** Specific suggestion

## Positive Observations
- [What was done well]

## Gaps
- [Edge cases or paths not covered]

## Open Questions
- [Low-confidence findings moved here by self-audit]

## Evidence
| Check | Result | Command | Output |
|-------|--------|---------|--------|
| Build | pass/fail | `dotnet build` | [summary] |
```

## Skill Authority

Skills in `.claude/skills/` are the authoritative source for patterns.

- **Skill exists, codebase has what it needs:** Follow the skill.
- **Skill exists, codebase missing something it references:** Build it. Escalate to planner only if scope seems too large.
- **No matching skill:** Planner may inline snippets. Log the missing skill in lessons.md.

# AI-Assisted Development Flow

This document describes the development methodology for building features using AI coding agents. The flow works with any agentic coding tool — Claude Code, Codex CLI, GitHub Copilot in Visual Studio — because it's built on conventions and role separation, not tool-specific features.

## Overview

Features are developed through a structured pipeline with two distinct AI agent roles — Architect and Developer — that mirror a real-world team dynamic. The Architect designs, the Developer implements, and the Architect reviews. Human oversight is present at every decision point.

## Prerequisites

The following foundation is put in place before the first feature:

- **Project instructions file** (CLAUDE.md / AGENTS.md / copilot-instructions.md) — project-level instructions covering repo structure, core principles, and pointers to conventions. This is the AI's onboarding document.
- **Agent role files** (Architect + Developer) — role definitions that constrain what each agent can and cannot do. Behavior only — no domain knowledge inline.
- **Convention files** — shared rules both agents follow: architecture rules, business context, coding standards, tech stack, and the agents workflow contract (plan format, done format, review checklist).
- **Skills** — reusable recipes encoding established codebase patterns. Skills ensure AI-generated code follows project conventions without re-discovering them each time.
- **Architecture reference** — comprehensive architecture documentation the AI can reference during planning.

## The Flow

### Step 1 — Describe the Feature

The human activates the Architect and describes the feature. The spec can come from anywhere — a Jira ticket, a Confluence page, a conversation, or a simple description typed into the terminal. For features with existing specs, the Architect reads them from the plan folder before asking questions.

### Step 2 — Architect Asks Questions (One Batch)

The Architect reads all available context — specs, architecture reference, convention files — and asks every clarifying question in one batch. Not one question at a time. One round of questions, one round of answers, then planning starts. This matters for efficiency: the human gets one batch of questions, answers them all, and the Architect has everything it needs.

### Step 3 — Architect Creates the Implementation Plan

The Architect produces a detailed plan saved to the plans folder. The plan covers: context, scope, domain model changes, data model changes, numbered implementation steps (each with file paths, pattern references, and skill references), cross-service changes, migration notes, testing strategy, and open questions.

Key principle: the plan is explicit. It specifies what to do, which files to touch, and which existing code to follow as examples. The Developer will read instructions literally — ambiguity produces wrong code.

### Step 4 — Human Reviews the Plan

The human reviews the plan, adds inline notes, asks questions, and requests revisions. This is the primary design checkpoint — it's cheaper to fix a plan than to fix implemented code.

The Architect revises until the plan is approved.

### Step 5 — Developer Asks Questions (One Batch)

The Developer reads the entire plan before touching any code. If anything is unclear — missing files, ambiguous instructions, patterns that can't be found — all questions are asked in one batch. The human either answers directly or forwards questions to the Architect.

### Step 6 — Developer Implements Step by Step

The Developer implements the plan one step at a time with explicit tracking:

- Before each step: re-read it from the plan (don't work from memory)
- After each step: announce "Step N done. Moving to Step N+1: {step name}"
- If a step is ambiguous or references something that doesn't exist: stop and ask
- After all steps: write a done file summarizing what was built

Key principle: the Developer treats the plan as a contract. It doesn't deviate silently — if something seems wrong, it stops and asks.

### Step 7 — Review (Staged Approach)

Review uses a staged approach to balance quality and cost:

**Developer self-review first** — the Developer re-reads the plan and verifies each step was completed. This catches mechanical issues: missed files, wrong naming, build failures, skipped steps. Cheap, and Sonnet-class models handle checklist work well.

**Architect review when needed** — for important features, pass the done file to the Architect. The Architect runs a two-step gate:

1. **Done check (no code reading)** — cross-reference every plan step against the done file. If steps are missing, send the Developer back without reading code.
2. **Code review (only if done check passes)** — re-read the plan, then verify each instruction has corresponding correct code. Flag all deviations.

This saves expensive reasoning tokens. Most Developer self-review findings are trivial. The Architect spends its tokens on architectural judgment, not pointing out a missing file.

### Step 8 — Fix, Iterate, Merge

The Developer fixes any issues from the review. The cycle repeats until approved. After final approval, lessons learned are captured and fed back into the system.

## Artifacts Per Feature

Each feature produces a folder with these files:

| File | Author | Purpose |
|---|---|---|
| `spec.md` | Human / Architect | What to build (requirements, acceptance criteria) |
| `plan.md` | Architect | How to build it (the contract) |
| `done.md` | Developer | What was built, key decisions, deviations |
| `review.md` | Architect | Review findings, conformance checklist |
| `lessons.md` | Both | Discoveries to feed back into the system |

## Knowledge Capture

After each implementation round, lessons are captured — patterns discovered, conventions not documented, skill gaps, plan ambiguities. Periodically, the Architect processes all lessons files and recommends where each item should go: project instructions, a convention file, a skill, or an agent file.

This is how the system improves over time. Convention files aren't static documents — they evolve through real implementation experience.

## What This Gives You

- **Separation of concerns** — design decisions are made before code is written, not during
- **Traceability** — every implementation decision traces back to a plan, which traces back to a spec
- **Consistency** — skills encode patterns once; every feature follows them
- **Quality gate** — staged reviews catch issues at the right cost level
- **Knowledge capture** — lessons flow back into the system, making each cycle better
- **Tool portability** — conventions and role separation work across any agentic coding tool
- **Cross-session continuity** — plan files survive session resets, context compaction, and machine switches. The context window does not.

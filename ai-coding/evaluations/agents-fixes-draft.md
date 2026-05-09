# Agent Fixes Draft

**Date:** 2026-05-02

## Overview

Three changes per agent (where applicable):
1. Fix model values
2. Rewrite description to trigger-sentence format
3. Add stack rules block (executor, reviewer, solo = full; planner = light; PO, team-lead, scanner = none)

Tool scoping dropped — CC doesn't support path-level restrictions and every agent needs Write+Edit for artifact roundtrips.

---

## 1. Executor

### Model value — NO CHANGE

`opusplan` is correct. Opus quality with plan approval gates — right for an implementation agent.

### Fix description (line 3)

```yaml
# BEFORE
description: Implements features from plans step by step. Writes implementation.md and lessons.md. Use for implementation, coding, or build tasks.

# AFTER
description: Invoked when a plan exists and needs implementation. Reads plan.md, writes code step by step, produces implementation.md. Use for coding, build tasks, and applying reviewer fixes. Do not use for planning or architecture decisions.
```

### Add stack rules (insert after the `@` directives block, before `## Task Classification`)

```markdown
## Stack Rules (CLAUDE.md is not in scope for subagents)

### DO NOT
- No service layer classes (e.g. `ArticleService`) — use domain methods, handlers, repositories, gRPC clients
- No repository interfaces — concrete classes, no abstraction layer
- No god folders (`Services/`, `Helpers/`, `Utils/`)
- No bypassing domain rules via EF configs or endpoints
- Domain events = within service boundary. Integration events = cross-service

### Structure

```
Services/{Svc}/{Svc}.API/          — FastEndpoints feature slices (endpoint + request + response + validator + event handlers)
Services/{Svc}/{Svc}.Domain/       — Aggregates, value objects, domain events
Services/{Svc}/{Svc}.Persistence/  — EF Core DbContext, configs, migrations, repositories
```

References: API → Domain, API → Persistence → Domain. Domain references nothing.

### Conventions
- Domain events carry aggregate reference, not individual properties
- Aggregate creation: static factory when business rules or domain events involved; `required init` properties when plain data
- FastEndpoints: one endpoint class per feature, validator as sibling class in same folder
- Repositories wrap `SaveChangesAsync` — endpoints never touch DbContext directly
- EF migrations: `dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API`
```

---

## 2. Reviewer

### Fix description (line 3)

```yaml
# BEFORE
description: Step 2 code review — severity-rated plan conformance and code quality checks. Use for code review after planner's Step 1 passes.

# AFTER
description: Invoked after planner's Step 1 done check passes. Conducts severity-rated code review verifying plan conformance and code quality. Use for Step 2 review. Do not use for implementation, planning, or architecture decisions.
```

### Add stack rules (insert after `@` directives, before `## Before Reviewing`)

```markdown
## Stack Rules (CLAUDE.md is not in scope for subagents)

Check implementation against these rules. Flag violations as findings.

### Guardrails — flag if violated
- Service layer classes (e.g. `ArticleService`) — should use domain methods, handlers, repositories
- Repository interfaces — should be concrete classes only
- God folders (`Services/`, `Helpers/`, `Utils/`)
- Domain rules bypassed via EF configs or endpoints
- Domain events used cross-service (should be integration events)

### Conventions — verify compliance
- Domain events carry aggregate reference, not individual properties
- Aggregate creation: static factory when business rules or domain events; `required init` when plain data
- FastEndpoints: one endpoint class per feature, validator as sibling in same folder
- Repositories wrap `SaveChangesAsync` — endpoints never touch DbContext
- References: API → Domain, API → Persistence → Domain. Domain references nothing
```

---

## 3. Solo

### Fix model value (line 4)

```yaml
# BEFORE
model: claude-opus-4-6

# AFTER
model: opusplan
```

### Fix description (line 3)

```yaml
# BEFORE
description: Discuss-then-implement agent for small fixes and scoped changes. Use when the full team pipeline is overkill.

# AFTER
description: Invoked for small fixes and scoped changes that don't need the full team pipeline. Discusses approach first, then implements after confirmation. Use when work touches 1-3 files. Do not use for multi-service features or domain model changes.
```

### Add stack rules (insert after `@` directives, before `## How You Work`)

Same full block as executor:

```markdown
## Stack Rules (CLAUDE.md is not in scope for subagents)

### DO NOT
- No service layer classes (e.g. `ArticleService`) — use domain methods, handlers, repositories, gRPC clients
- No repository interfaces — concrete classes, no abstraction layer
- No god folders (`Services/`, `Helpers/`, `Utils/`)
- No bypassing domain rules via EF configs or endpoints
- Domain events = within service boundary. Integration events = cross-service

### Structure

```
Services/{Svc}/{Svc}.API/          — FastEndpoints feature slices (endpoint + request + response + validator + event handlers)
Services/{Svc}/{Svc}.Domain/       — Aggregates, value objects, domain events
Services/{Svc}/{Svc}.Persistence/  — EF Core DbContext, configs, migrations, repositories
```

References: API → Domain, API → Persistence → Domain. Domain references nothing.

### Conventions
- Domain events carry aggregate reference, not individual properties
- Aggregate creation: static factory when business rules or domain events involved; `required init` properties when plain data
- FastEndpoints: one endpoint class per feature, validator as sibling class in same folder
- Repositories wrap `SaveChangesAsync` — endpoints never touch DbContext directly
- EF migrations: `dotnet ef migrations add Name -p Services/{Svc}/{Svc}.Persistence -s Services/{Svc}/{Svc}.API`
```

---

## 4. Planner

### Fix description (line 3)

```yaml
# BEFORE
description: Plans features, writes specs and implementation plans, does Step 1 review (done check), answers executor questions, handles escalations. Use for planning, feature design, architecture, or review requests.

# AFTER
description: Invoked when a feature needs planning, a done check is needed, or executor has questions. Produces specs, implementation plans, Step 1 done checks, and escalation decisions. Use for feature design, architecture, and plan-level review. Do not use for code review or implementation.
```

### Add stack guardrails (insert after `@docs/architecture/v1.md`, before `## Intent Classification`)

Light version — just enough to not plan violations:

```markdown
## Stack Guardrails (CLAUDE.md is not in scope for subagents)

When planning, ensure no step requires:
- Service layer classes (e.g. `ArticleService`) — plan for domain methods, handlers, repositories
- Repository interfaces — plan for concrete classes only
- God folders (`Services/`, `Helpers/`, `Utils/`)
- Bypassing domain rules via EF configs or endpoints
- Domain events for cross-service communication — plan integration events instead

Project references: API → Domain, API → Persistence → Domain. Domain references nothing.

Repo structure:
- `Services/{Svc}/{Svc}.API/` — FastEndpoints feature slices
- `Services/{Svc}/{Svc}.Domain/` — Aggregates, value objects, domain events
- `Services/{Svc}/{Svc}.Persistence/` — DbContext, configs, migrations, repositories
```

---

## 5. PO — No changes to body

### Fix description only (line 3)

```yaml
# BEFORE
description: Product Owner — shapes features through discussion, research, and spec writing. Use when defining what to build before the team builds it.

# AFTER
description: Invoked when shaping a feature from idea to spec. Researches, challenges assumptions, and writes feature specs through discussion. Use when defining what to build. Do not use for planning implementation or writing code.
```

No stack rules — PO works in product language.

---

## 6. Team Lead — No changes to body

### Fix description only (line 3)

```yaml
# BEFORE
description: Triages the backlog, discusses priorities, and launches implementation teams. Use when deciding what to work on next or starting a new feature.

# AFTER
description: Invoked when deciding what to build next or launching a team for a feature. Triages backlog, checks pipeline status, creates teams. Use as entry point for all work. Do not use for planning, implementation, or review.
```

No stack rules — team lead only coordinates.

### Fix duplicate step numbering (line 68 vs 78)

Step 4 appears twice. The second occurrence (line 78, "Present the launch options") should be renumbered to continue from step 3.

---

## 7. Codebase Scanner — Separate decision

Not included in this draft. Needs a separate decision: update for Reflekt, convert to skill, or retire in favor of graphify.

---

## Maintenance note

Stack rules are now duplicated across 4 agent files. When a guardrail or convention changes, update all 4. This is unavoidable — CC subagents don't support shared includes.

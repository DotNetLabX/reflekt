# Workflow Conventions

Shared contracts between Architect and Developer agents. Both agents follow these formats.

## Implementation Plan Format

Plans are saved to `ai-coding/plans/{FeatureName}/plan.md` by the Architect. The Developer follows them step by step.

```
# {Feature Name}

## Context
What problem this solves. Which service(s) are impacted and why.

## Scope
In scope. Explicitly out of scope.

## Domain Model Changes
New/modified aggregates, entities, value objects, domain events.

## Data Model Changes
New tables, columns, relationships, migrations needed.

## Implementation Steps
Numbered steps — each step is one focused task the Developer can execute.
For each step specify:
- What to do (not how to code it)
- Which files to create or modify (full paths)
- What pattern to follow (reference an existing file as example)
- Which skill to use if one applies
- Dependencies on previous steps

## Cross-Service Changes (if applicable)
gRPC contract changes, new integration events, new consumers.

## Migration Notes
EF Core migration commands. Seed data if needed.

## Testing Strategy
Key scenarios to test.

## Open Questions
Unresolved decisions that need input before proceeding.
```

## Done File Format

After each implementation round, the Developer writes `ai-coding/plans/{FeatureName}/done.md`:

```
# {Feature Name} — Done

## Files Created
- `full/path/to/File.cs` — one line summarizing what it does

## Files Modified
- `full/path/to/File.cs` — what changed and why

## Key Decisions
- Any implementation choices not specified in the plan

## Deviations from Plan
- Any steps done differently than planned, with reasons
```

## Lessons File Format

Both agents write to `ai-coding/plans/{FeatureName}/lessons.md`:

```
# {Feature Name} — Lessons

## Developer Lessons
- Patterns discovered in the codebase not yet documented
- Inconsistencies found
- Conventions not documented
- Steps missing from a skill

## Architect Lessons
- Plan instructions that were ambiguous or caused confusion
- Architecture decisions that need documenting
- Skill gaps discovered
```

Only add items not already captured in CLAUDE.md, convention files, architecture-reference.md, skills, or agent files.

Update before `/compact` or `/clear` to capture anything not yet written down.

## Review Checklist

The Architect uses this when reviewing Developer output. The Developer should be aware of what gets checked.

### Step 1: Done check (no code reading)
- Every plan step has a corresponding entry in done.md
- No plan steps are missing or silently skipped
- Reported deviations have reasons
- No unexpected files or scope creep

### Step 2: Code review (only if Step 1 passes)
- Each plan instruction has corresponding code that matches
- Referenced patterns and skills were actually followed
- Naming conventions match coding standards
- Build passes
- No new patterns invented that aren't in the codebase
- Domain rules enforced in aggregates, not in handlers or endpoints
- Deviations flagged — either the plan is updated or the code is corrected

Review findings are saved to `ai-coding/plans/{FeatureName}/review.md`. Once saved, the Developer reads it and addresses each item.

## Skill Authority

Skills in `.claude/skills/` are the authoritative source for patterns. They define what the codebase should look like.

- **Skill exists, codebase has what it needs:** Follow the skill.
- **Skill exists, codebase is missing something it references** (base class, type, dependency): Build it. The skill defines what should exist — if `Repository<T>` doesn't exist yet, create it. Escalate to the Architect only if the scope seems too large, not because the thing is missing.
- **No matching skill for the task:** The Architect may inline snippets as a one-time guide. Log the missing skill in `lessons.md` (name, what it should cover, reference files) so it can be created later.

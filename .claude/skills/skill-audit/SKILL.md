---
name: skill-audit
description: Audits codebase conformance against skill definitions. Scans files that should follow a skill's patterns, reports deviations with severity ratings. Use when skills were updated or to verify codebase consistency.
---

# Skill Audit

Scans the codebase for files governed by a skill and checks whether they follow the skill's prescribed patterns. Produces a conformance report with severity-rated deviations.

## Arguments

`/skill-audit {skill-name}` — audit against a single skill.
`/skill-audit all` — audit against all code-generation and pattern skills.

## Steps

1. **Load the skill definition.**
   - Read `.claude/skills/{skill-name}/SKILL.md` and all files in `workflows/` and `references/`.
   - Extract the structural rules: file locations, naming conventions, class inheritance, code organization, required sections.

2. **Build the file manifest.**
   From the skill's documented file patterns (e.g., `Features/{Domain}/{Name}/{Name}Endpoint.cs`), scan the codebase for all files that should conform. Use Glob with the skill's path patterns. If the skill doesn't specify explicit paths, infer from:
   - Folder structure patterns mentioned in the skill
   - Class base types or interfaces prescribed by the skill (grep for `Endpoint<`, `AggregateRoot`, `Validator<`, etc.)

3. **Check each file against the skill's rules.**

   For each file in the manifest, check applicable rules:

   | Category | What to check |
   |----------|---------------|
   | **Location** | File is in the prescribed directory structure |
   | **Naming** | File and class names follow the skill's naming convention |
   | **Structure** | Class inherits from the prescribed base type |
   | **Co-location** | Related files (command + validator, endpoint + command) are together per the skill |
   | **Content** | Key structural elements are present (e.g., validator has rules, endpoint calls domain, handler follows prescribed flow) |
   | **Adaptation** | Project-specific adaptation notes in the skill are respected (e.g., correct auth approach, correct validation extensions) |

   Skip checks that don't apply to a given skill type (e.g., don't check co-location for pattern-only skills).

4. **Classify findings by severity.**

   | Severity | Criteria |
   |----------|----------|
   | **CRITICAL** | Silent runtime failure — code compiles but behavior is wrong (e.g., polymorphic dispatch broken, events lost, empty collections) |
   | **HIGH** | Structural violation — wrong base class, wrong file location, missing required element |
   | **MEDIUM** | Convention deviation — naming mismatch, co-location broken, suboptimal pattern used |
   | **LOW** | Style preference — minor inconsistency that doesn't affect behavior |

5. **Produce the conformance report.**
   Save to `docs/audits/{skill-name}-audit.md`. Create the `docs/audits/` directory if it doesn't exist.

6. **Summarize and recommend.**
   - If CRITICAL or HIGH findings: recommend a fix plan (architect creates plan, developer implements).
   - If only MEDIUM/LOW: list as optional improvements, no plan needed.
   - If clean: report conformance and move on.

## Report Format

```markdown
# Skill Audit — {Skill Name}

## Summary
- **Skill:** {name} ({one-line description})
- **Files scanned:** {count}
- **Conformant:** {count}
- **Deviations:** {count by severity}

## Findings

### [{SEVERITY}] {Finding title}
**Files:** `path/to/file.cs` (+ others if pattern-wide)
**Rule:** What the skill prescribes
**Actual:** What the code does
**Fix:** Specific change needed

## Conformant Files
- `path/to/good/file.cs` — follows {pattern}

## Recommendations
- [ ] {Action items, if any}
```

## Which Skills to Audit

Not all skills are auditable — pattern/convention skills describe approaches, not enforceable file structures. Focus on skills with concrete file patterns:

| Auditable | Skills |
|-----------|--------|
| **Code generation** | `create-feature`, `create-aggregate`, `create-domain-event-handler`, `create-service`, `create-module`, `add-integration-event`, `create-grpc-contract`, `add-pipeline-behavior`, `extract-feature-service` |
| **Persistence** | `persistence-patterns` (entity configs, repositories) |
| **Frontend** | `create-vue-feature`, `vue-component-architecture` |

| Not auditable (guidance-only) | Skills |
|-------------------------------|--------|
| Process | `create-implementation-plan`, `create-feature-spec`, `create-architecture-doc`, `create-module-claude-md`, `create-service-claude-md` |
| Reference | `domain-patterns`, `cqrs-patterns`, `error-handling`, `authorization-patterns`, `redis-patterns`, `system-design` |
| Meta | `improve-flow`, `improve-skills`, `skill-audit` |

## Integration with Pipeline

This skill does not fix code. It produces a report that feeds into:
- **Architect** — reads the report, creates a fix plan for CRITICAL/HIGH items
- **Developer** — implements fixes per the plan
- **Reviewer** — verifies fixes match both the plan and the skill

For MEDIUM/LOW items with no plan, the developer can fix them opportunistically when touching those files.

## What This Skill Does NOT Do

- Fix code — it only reports.
- Audit application logic or business rules — only structural conformance.
- Check files that don't belong to any skill's domain.
- Replace the reviewer — the reviewer checks plan conformance, this checks skill conformance.

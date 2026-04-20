# Foundation — Skill Alignment Refactor

## Context

The Reflekt service was scaffolded without following the `create-service` skill. BuildingBlocks base types, GlobalUsings, and project structure may be incomplete. This must be resolved before feature-level refactors (Teams) can align cleanly with their own skills.

## Steps

### Step 1: Align with create-service skill

Re-read the `create-service` skill and all its workflows. Compare the current Reflekt service (API, Domain, Persistence) and BuildingBlocks against each workflow. Align any deviations.

### Step 2: Verify

Build passes. Existing unit and integration tests pass.

# Claude Code Setup

How Claude Code is configured in this project. This covers the file structure, agent roles, skills, conventions, and workflows that make up the AI-assisted development setup.

## File Structure

```
CLAUDE.md                              # Project instructions — loaded automatically every session
.claudeignore                          # Files/dirs Claude Code should never read
ai-coding/
  agents/
    Architect.md                       # Architect agent role (plans, reviews, no code)
    Developer.md                       # Developer agent role (implements from plans)
  conventions/
    architecture-rules.md              # DDD, vertical slice, CQRS, non-negotiable rules
    business-context.md                # Article lifecycle, actors, service boundaries, event flow
    coding-standards.md                # Naming, patterns, guardrails, code style
    tech-stack.md                      # Stack details, build/test commands, DB choices
    agents-workflow.md                 # Shared contracts: plan format, done format, lessons, review checklist
  plans/
    {FeatureName}/                     # One folder per feature
      spec.md                          # What to build (written by human or Architect from prompts)
      plan.md                          # How to build it (Architect writes)
      done.md                          # What was built (Developer writes)
      lessons.md                       # What was learned (both agents write)
      review.md                        # Review findings (Architect writes)
.claude/
  skills/
    {skill-name}/                      # One folder per skill (auto-discovered by Claude Code)
      SKILL.md                         # Skill entrypoint with YAML frontmatter
      workflows/                       # Optional multi-step sub-files
        1-step-name.md
        2-step-name.md
docs/
  architecture-reference.md            # Full architecture doc with code examples
  db-schema.csv                        # Database schema (all tables and columns)
```

## How It Works

### Two-Agent Workflow

The setup uses two agent roles with a clear separation:

**Architect** (`ai-coding/agents/Architect.md`) — loaded with `read ai-coding/agents/Architect.md`
- Runs in Terminal 1: `claude --model opus`
- Behavior only — no domain knowledge in the agent file, all knowledge lives in conventions
- Imports: `architecture-rules.md`, `business-context.md`, `agents-workflow.md`
- Discusses features, asks all clarifying questions in one batch before planning
- Produces implementation plans in `ai-coding/plans/{FeatureName}/plan.md`
- Reviews Developer output via two-step gate (done check → code review)
- Never writes implementation code
- Reads skill filenames only (not full content) to reference in plans

**Developer** (`ai-coding/agents/Developer.md`) — loaded with `read ai-coding/agents/Developer.md`
- Runs in Terminal 2: `claude --model opusplan`
- `opusplan` = Opus for planning (Shift+Tab), Sonnet for implementation — automatic switching
- Imports all 5 convention files (cost doesn't matter for Developer, only output quality)
- Reads plan fully, asks all questions in one batch before implementing
- Implements step by step with explicit tracking: "Step N done. Moving to Step N+1: {name}"
- Reads full skill content and follows established patterns
- Writes done/lessons files after each implementation round

Model choice rationale: Architect uses Opus because architecture decisions require the highest reasoning capability. Developer uses `opusplan` — Opus handles planning when toggled via Shift+Tab, Sonnet handles implementation. This gives the Developer access to Opus-level reasoning when needed while conserving token budget for routine code generation. This matters especially on Pro plans where limits are tight.

### Agent Design Principles

- **Agent files are behavior only** — they define how the agent acts, not what the system looks like. Domain knowledge, architecture rules, and coding standards live in convention files.
- **Conventions are imported selectively** — the Architect imports 3 convention files (doesn't need coding standards or tech stack). The Developer imports all 5.
- **Both agents front-load questions** — ask all clarifying questions in one batch before starting real work (planning or implementing). No mid-plan or mid-implementation surprises.
- **Both agents end with a ready signal** — "Architect ready." / "Developer ready." Suppresses summary token waste.

### Convention Files

Convention files live in `ai-coding/conventions/` and contain the detailed rules that were too large for root CLAUDE.md. Each agent file imports only the convention files it needs via `@` references:

| File | What it contains | Architect | Developer |
|---|---|---|---|
| `architecture-rules.md` | DDD, vertical slice, CQRS, domain model patterns, guardrails | ✓ | ✓ |
| `business-context.md` | Article lifecycle, actors, service boundaries, event flow, communication patterns | ✓ | ✓ |
| `coding-standards.md` | Naming conventions, file structure, code-level guardrails | — | ✓ |
| `tech-stack.md` | Stack per service, DB choices, build/test/run commands, port conventions | — | ✓ |
| `agents-workflow.md` | Shared contracts: plan format, done format, lessons format, review checklist | ✓ | ✓ |

#### Why `ai-coding/conventions/` Instead of `.claude/rules/`

CC supports `.claude/rules/` — any `.md` file dropped there auto-loads every session with the same priority as CLAUDE.md. No `@` references needed.

The trade-off: `.claude/rules/` is zero-config and good for teams where everyone runs the same general-purpose CC session. But it has no per-agent scoping — ALL rules load into ALL sessions, wasting tokens when the Architect doesn't need coding standards or the Developer doesn't need architecture decision guidelines.

With `ai-coding/conventions/` + `@` references, each agent file cherry-picks exactly what it needs. More precise context management, fewer tokens per session.

Use `.claude/rules/` when: same team, no agent roles, everyone gets everything.
Use `ai-coding/conventions/` when: agent roles with different context needs, or portability to other tools matters.

### Windows Terminal Profiles

Pre-configured profiles for one-click agent startup:

- **CC Architect**: starts `claude --model opus` in the repo root, titled "Architect"
- **CC Developer**: starts `claude --model opusplan` in the repo root, titled "Developer"
- Default terminal profile is unchanged — kept for normal terminal work

### Session Start Routine

1. Click the terminal profile (Architect or Developer)
2. First message: `read ai-coding/agents/Architect.md` (or `Developer.md`)
3. Agent responds with "Architect ready." (or "Developer ready.")
4. `/rename` based on the current git branch (e.g., `feature/create-article` → `/rename CreateArticle-Submission`)
5. Scope the session: "We're working on the Submission service"

### Typical Flow

1. Load Architect: `read ai-coding/agents/Architect.md`
2. Describe the feature — Architect asks all questions in one batch
3. Answer questions — Architect writes plan to `ai-coding/plans/{FeatureName}/plan.md`
4. Review and annotate the plan — repeat until approved
5. Switch to Developer terminal, load: `read ai-coding/agents/Developer.md`
6. Point Developer at the plan — Developer asks all questions in one batch
7. Answer questions (or forward to Architect) — Developer implements step by step
8. Developer writes `done.md` and `lessons.md`
9. Review options (staged approach):
   a. **Developer self-review first** — Developer re-reads plan and verifies each step was completed (cheap, catches mechanical issues)
   b. **Architect review when needed** — pass `done.md` to Architect for two-step review gate (expensive, catches architectural issues)
10. Fix, iterate, merge

### Review Process

**Option C — staged review** (recommended):

Developer catches mechanical stuff first — missed files, wrong naming, build failures, skipped steps. That's checklist work Sonnet handles well. Then the user decides: is this feature important enough for an Architect review? If yes, pass to Architect. The Architect's two-step gate:

1. **Done check (no code reading)** — cross-reference every plan step against done.md. If items are missing, send Developer back. Don't read code yet.
2. **Code review (only if done check passes)** — re-read the plan, then verify each instruction has corresponding correct code. Flag all deviations.

This saves Opus tokens — most Developer self-review findings are trivial. The Architect spends expensive tokens on architectural judgment, not pointing out a missing file.

### Knowledge Capture

After each implementation round, the Developer updates:
- **`ai-coding/plans/{FeatureName}/done.md`** — files touched with one-line summaries, key decisions, deviations from plan
- **`ai-coding/plans/{FeatureName}/lessons.md`** — patterns, inconsistencies, or conventions discovered that aren't already documented

The Architect also writes lessons (under `## Architect Lessons` in the same file) after review cycles.

Periodically, ask the Architect to "process lessons" — it reads all `ai-coding/plans/*/lessons.md` files and recommends where each item should go (CLAUDE.md, a convention file, architecture-reference.md, a skill, or an agent file). Changes are applied only after approval. This is how the convention files improve over time instead of staying static.

## CLAUDE.md

The main project instructions file. Loaded automatically at the start of every Claude Code session. Kept minimal — shared context only:

- Project description (2-3 lines)
- Repo structure overview
- BuildingBlocks overview (needed by both agents)
- Core principles (DDD, vertical slice, CQRS — names only, details in convention files)
- Pointer to `ai-coding/` (agents, conventions, plans)
- Compaction rules
- "Don't re-read files" rule to reduce token waste

Target: under 100 lines. Everything else goes in convention files or service-level CLAUDE.md files.

### Progressive Build Strategy

The CLAUDE.md starts minimal and grows across lectures. At M2L3 it contains ONLY:
- Project description (2-3 lines)
- Repo structure (src/ tree with descriptions)
- Core principles (DDD, Vertical Slice, CQRS — names only, no explanations)
- Architecture guardrails (what NOT to do)
- EF Core migration commands (the path pattern is project-specific)

Added later as those topics are introduced:
- Pointer to `ai-coding/` → when agents are taught (M2L6)
- Agent loading instructions → when agents are taught (M3)
- Skills reference → when skills are taught (M3)
- Compaction rules → when session management is taught (M3)
- File re-read rule → when token efficiency is taught (M3)
- Port conventions → when building first service (M3)

What does NOT go in root CLAUDE.md:
- Naming conventions → `ai-coding/conventions/coding-standards.md` (Developer agent only)
- `dotnet build`, `dotnet test`, `docker compose up -d` → CC already knows these
- .claudeignore contents → covered in M2L5 but not referenced from CLAUDE.md

### Service-Level CLAUDE.md Files

Auto-loaded when CC works in a service folder (hierarchical loading — root + service):

```
src/Services/Submission/CLAUDE.md      # DB choice, domain model, endpoints, service-specific rules
src/Services/Auth/CLAUDE.md            # JWT config, user model, auth-specific patterns
src/Services/Review/CLAUDE.md          # Review-specific context
src/Services/ArticleHub/CLAUDE.md      # GraphQL, Postgres, consumer patterns
```

## .claudeignore

Prevents Claude Code from reading large or irrelevant files that waste context:

- `bin/`, `obj/` — build output
- `Migrations/` — EF Core migrations (generated, rarely relevant)
- `.vs/` — Visual Studio internals
- `*.lock` — lock files
- `docker-compose.override.yml` — local overrides

## Permissions (settings.local.json)

Project-level permissions stored in `.claude/settings.local.json`. Allow all standard tools, deny destructive commands:

```json
{
  "permissions": {
    "allow": [
      "Read", "Edit", "Write", "MultiEdit",
      "Bash", "Glob", "Grep", "LS",
      "WebFetch", "WebSearch", "TodoWrite", "Task"
    ],
    "deny": [
      "Bash(rm -rf /*)",
      "Bash(sudo *)",
      "Bash(git push*)",
      "Bash(docker rm*)"
    ]
  }
}
```

Add `.claude/settings.local.json` to `.gitignore` — NOT the whole `.claude/` folder, since skills and shared settings should be committed.

## Skills

Skills are reusable recipes for common development patterns. They encode established codebase conventions so the Developer agent produces consistent code without reinventing patterns.

Each skill uses YAML frontmatter for auto-discovery and slash command invocation:

```markdown
---
name: new-vertical-slice
description: Creates a new vertical slice feature with endpoint, request/response DTOs, validator, command/query, handler, and Mapster config for a specified service
argument-hint: [FeatureName] [ServiceName]
---

# New Vertical Slice

1. Create feature folder under `src/Services/{ServiceName}/Features/{FeatureName}/`
2. ...
```

The `name` becomes a slash command (`/new-vertical-slice`). The `description` tells CC when to auto-invoke the skill — CC reads all descriptions at startup and matches them to prompts. Set `disable-model-invocation: true` if you want manual-only invocation.

Multi-step skills use a `workflows/` subdirectory with numbered step files for progressive disclosure — CC loads steps one at a time as needed.

Skills are taught after students learn the manual workflow. The teaching arc: CC built-in capabilities first (Shift+Tab, natural prompts) → custom skills that formalize those patterns later.

## Architecture Reference

`docs/architecture-reference.md` is the comprehensive architecture document. It covers code examples, database patterns, domain model details, service boundaries, and the full article lifecycle. Both agents reference it for codebase patterns and conventions. It's the source material from which convention files are built.

## Feature Specs and Plans

**Specs** (`ai-coding/plans/{FeatureName}/spec.md`) are the functional and technical requirements — what to build and why. For early features (CreateArticle), the spec is built interactively through prompts. For later features (Review service), pre-written specs simulate real-world Jira/Confluence workflows.

**Plans** (`ai-coding/plans/{FeatureName}/plan.md`) are the Architect's implementation instructions — how to build it. They reference specific modules, existing code patterns, skills, and follow the format defined in `agents-workflow.md`.

Plans are the contract between Architect and Developer. The Developer follows them literally, which is why the Architect must be explicit about performance approach (bulk vs per-row), reference existing code as examples, and mention applicable skills.

Plans also serve as the **cross-session continuity layer**. After `/compact` or `/clear`, point CC back to the plan: "Read `ai-coding/plans/CreateArticle/plan.md` and continue from step 3." Plan files survive session resets, context compaction, and machine switches — the context window does not.

## Session Management

| Command | When to use |
|---|---|
| `/rename` | Immediately at session start — name after git branch |
| `/clear` | Between features — fresh context |
| `/compact` | Within a long feature, conversation getting heavy |
| `/compact Keep domain model decisions and current file changes` | Compact with preservation instructions |
| `/context` | Check what's consuming context window |
| `Shift+Tab` | Toggle plan mode (plan before executing) |
| `Escape` | Stop CC immediately when going wrong direction |

### Resuming Sessions

- `claude --resume` — interactive session picker (shows named sessions)
- `claude --resume session-name` — resume specific session
- `claude --continue` — resume most recent session
- `claude --from-pr <number>` — load a PR's diff as context (useful for Architect reviewing Developer's PR)

One session per feature. Sessions can span 1-3 days. Git-committed plan files are the real continuity layer, not sessions.

## Cost Efficiency (in order of impact)

1. **Minimal root CLAUDE.md + scoped convention files** — saves tokens on every message
2. **`.claudeignore`** — prevents CC from reading junk files
3. **`opusplan` for Developer, Opus for Architect** — Opus only when reasoning matters, Sonnet for implementation via automatic switching
4. **Staged reviews (Option C)** — Developer self-reviews first, Architect only when needed
5. **Specific prompts** — vague requests = expensive exploration
6. **`/clear` between features** — stops context bloat
7. **`/rename` + `--resume`** — session management across days
8. **`Escape` when CC goes wrong** — stops token burn immediately
9. **Plan mode (`Shift+Tab`) for complex tasks** — prevents expensive wrong-direction work

Track usage with `npx ccusage@latest` (run from any terminal, not inside CC):
- `npx ccusage@latest session` — per-session cost
- `npx ccusage@latest daily` — daily cost report
- `npx ccusage@latest blocks` — 5-hour billing window tracking

## Key Concepts Clarified

### Agents vs Subagents vs Agent Teams
- **Interactive sessions with role files** = what we call "agents" (not CC's official term) — two terminals, you interact directly, roles enforced via loaded agent files
- **Subagents** (`.claude/agents/`) = delegated workers spawned by main session, non-interactive, isolated context, return summary and disappear
- **Agent Teams** = multiple CC instances coordinating in parallel, each with own terminal pane (needs tmux/iTerm2), automated orchestration — advanced topic

### @import Behavior
- Official: only works in CLAUDE.md and files imported by CLAUDE.md (recursive, max 5 levels)
- Behavioral: `@` references in agent files work via Read tool — CC sees the reference and reads the file
- For our use case (loading once at session start): acceptable, no meaningful overhead

### Subscription Limits
- Max plan is NOT unlimited — has 5-hour rolling windows + weekly caps
- Opus burns through limits faster than Sonnet (~1.7x)
- Students will replicate everything — cost awareness is a design constraint, not just a topic
- Pro ($20/mo) will hit limits fast on a microservices project with two terminals

### Codex Portability
- Convention file content transfers directly to Codex
- Codex uses AGENTS.md, no `@import` support, no role files per session
- Hierarchical AGENTS.md per directory works same as CC's CLAUDE.md hierarchy
- Role separation = first prompt only, no file-based enforcement

### CC Re-reads Files
- Confirmed: CC re-reads same files repeatedly within a session — biggest token waste
- CLAUDE.md rule helps but not bulletproof (CC can ignore instructions as context fills up)
- `read-once` hook exists (60-90% reduction) — not using for now
- Track re-reads via JSONL at `~/.claude/projects/<project>/<session-id>.jsonl`

### Settings (in `~/.claude/settings.json`)

```json
{
  "env": {
    "CLAUDE_CODE_EFFORT_LEVEL": "medium",
    "DISABLE_NON_ESSENTIAL_MODEL_CALLS": "1"
  }
}
```

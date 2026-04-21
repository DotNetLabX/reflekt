# Multi-Agent Automation for .NET Product Development

Findings, decisions, and reasoning for automating the Architect→Developer pipeline. Focused on fast product development, not course delivery.

---

## The Goal

Build plans during the day. Let agents implement them in parallel or sequentially overnight. Wake up to implemented features with a full paper trail (spec → plan → done → review → lessons).

---

## Current Setup (Baseline)

Two-terminal manual workflow with clear role separation:

- **Terminal 1 — Architect** (`claude --model opus`): loads `Architect.md`, imports 3 convention files via `@` references, produces `plan.md`, reviews `done.md` via a two-step gate (done check → code review), never writes code.
- **Terminal 2 — Developer** (`claude --model opusplan`): loads `Developer.md`, imports all 5 convention files, implements plan step by step with build verification, writes `done.md` and `lessons.md`.
- **Human in the loop at every stage**: describe feature → answer questions → review plan → forward Developer questions → review output → iterate.
- **File-based handoff**: `spec.md` → `plan.md` → `done.md` → `review.md` → `lessons.md` per feature in `ai-coding/plans/{FeatureName}/`.
- **Convention files** in `ai-coding/conventions/` with selective `@` imports per agent role.
- **Skills** in `.claude/skills/` with YAML frontmatter for auto-discovery.

### What works well

- **Artifact discipline** — full traceability that survives compaction, session resets, and months of time.
- **Selective convention imports** — saves tokens (Architect: 3 files, Developer: 5).
- **Two-step review gate** — done check (cheap) before code review (expensive) saves Opus tokens.
- **Staged review** — Developer self-reviews first, Architect reviews only when needed.
- **Skill authority** — clear rule for when to follow vs when to build missing pieces.
- **Plan-as-contract** — reliable Developer output because ambiguity is resolved before implementation.

### What doesn't work

- **Human is the bottleneck** — every handoff requires human presence.
- **No overnight capability** — Developer stops between steps waiting for prompts.
- **No rate-limit recovery** — session sits idle until human returns.
- **Manual overhead** — two terminals, session loading, question forwarding.

---

## Tools Evaluated

### CC Agent Teams (native, experimental)

2-16 coordinated CC sessions with peer-to-peer messaging, shared task lists, and file locking.

**How it works:** Describe team in natural language → CC spawns teammates → each auto-loads CLAUDE.md, skills → teammates communicate via SendMessage tool → shared task list manages dependencies.

**Navigation:** `Shift+Down` (next teammate), `Shift+Up` (previous), `Ctrl+T` (task list), `Escape` (interrupt).

**Critical limitations for overnight:**

| Limitation | Impact |
|---|---|
| No session resume | Rate limit at 3am = teammates gone. Cannot recover. |
| No model routing | All teammates run Opus. No Architect-Opus + Developer-opusplan split. Everything at Opus cost. |
| Ephemeral | Teams don't survive session end. Must re-describe every session. |
| Windows: no split-pane | Needs tmux/iTerm2. Windows limited to in-process mode. |

**Verdict:** Good for daytime supervised parallel bursts. Not viable for overnight.

### oh-my-claudecode (OMC)

Open-source plugin adding multi-agent orchestration. 19 agents, auto-resume, model routing, HUD.

**How it works:** Plugin writes orchestration instructions to `.claude/CLAUDE.md` → magic keywords trigger modes via hooks → CLAUDE.md tells CC "delegate everything" → CC routes tasks to OMC agents by description matching.

**Three features that matter:**

1. **`omc wait --start`** — auto-resume daemon monitors rate limits, resumes automatically.
2. **Model routing** — Opus for planning, Sonnet for implementation, Haiku for lookups. 30-50% savings claimed.
3. **Ralph mode** — persistent loop: implement → verify → fix → repeat until done.

**Problems for this setup:**

| Problem | Why it matters |
|---|---|
| Generic microservice agents | 19 agents split what our Architect handles as one. Router expects narrow agents, not lifecycle-aware monoliths. |
| No selective convention imports | Every agent sees everything. Token overhead, less focused context. |
| `.omc/skills/` auto-learning | Can silently encode bad patterns from overnight runs. Requires weekly audit. |
| No artifact structure | Writes to `.omc/plans/`, not `ai-coding/plans/`. No done.md, review.md, lessons.md by default. |
| Aggressive CLAUDE.md injection | Conductor instructions, mode detection, keyword triggers loaded every session — even simple ones. |
| Weekly releases | Can break on update. Must pin version and test. |
| Agent replacement is fragile | `.claude/agents/` overrides by name, but OMC routing assumptions change between versions. |

**OMC agents vs this setup's Architect (side-by-side):**

| This setup's Architect does | OMC agent(s) | Quality gap |
|---|---|---|
| Discuss features, ask questions | analyst | Generic, no project knowledge |
| Write implementation plans | planner | 3-6 steps with acceptance criteria. No file paths, no skill refs, no performance approach. |
| Challenge/review plans | critic | Not needed — human reviews plans |
| Done check (Step 1) | verifier | Generic checklist. No two-step gate. |
| Code review (Step 2) | code-reviewer | Generic. No plan-as-contract concept. |
| Architecture analysis | architect | READ-ONLY code analysis. Good but solves different problem. |

OMC's planner produces generic plans. This setup's Architect produces plans with full file paths, pattern references, skill references, performance approach, domain model changes, migration notes. The quality gap is significant.

**Verdict:** Auto-resume, model routing, and ralph are valuable. Agent system and orchestration philosophy are incompatible with this setup.

### Superpowers

Skills framework: brainstorm → design → plan → implement (TDD) → verify.

**How it works:** 14 skills installed via plugin system. Does NOT write to `.claude/CLAUDE.md`. Master skill auto-dispatches based on task type via CC native skill matching.

**Assessment:** This setup already has everything Superpowers provides in customized form. Architect's batch-questions + spec workflow = brainstorming. Plan format with file paths + skill refs = planning. Developer agent = execution. Two-step review = verification.

**Verdict:** Not needed. This setup IS the custom-built version of what Superpowers provides generically.

---

## How Agent Triggering Works in CC

1. **Discovery** — CC reads all `.claude/agents/*.md` at session startup, loads `description` fields.
2. **Matching** — CC matches user intent against descriptions to decide delegation. The `description` is a routing rule, not a label.
3. **Precedence** — session-defined > project `.claude/agents/` > user `~/.claude/agents/` > plugin agents.
4. **Frontmatter controls** — `name` (invocation), `description` (trigger), `model` (which model runs it), `disallowedTools` (hard enforcement).
5. **Conductor instruction** — with "delegate everything" in CLAUDE.md, CC proactively delegates. Without it, CC only delegates on strong description match.

**Implication:** Automation needs either (a) always-on conductor in CLAUDE.md (affects every session) or (b) explicit commands (requires manual triggering). The custom approach uses selective delegation — delegate for substantial work, handle simple questions directly.

---

## Comparative Evaluation

| Category | Weight | Current Setup | OMC | Agent Teams |
|---|---|---|---|---|
| Overnight viability | High | 2 | 5 | 2 |
| Output quality | High | 5 | 3 | 4 |
| Artifact discipline | High | 5 | 2 | 4 |
| Parallelism | Medium | 2 | 4 | 4 |
| Review quality | Medium | 5 | 3 | 3 |
| Token economics | Medium | 4 | 2 | 2 |
| Stability / risk | Medium | 5 | 2 | 3 |
| Debuggability | Medium | 5 | 2 | 3 |

Current setup dominates everything except overnight viability. The automation gap is the only reason to change anything.

---

## Decision: Build Custom Automation on CC Native Features

### Why not OMC

- Agents are generic microservices for an automated router. This setup's Architect is a monolith owning the full lifecycle. Incompatible philosophies.
- CLAUDE.md injection is always-on overhead.
- Auto-learning can encode bad patterns. Weekly audit burden.
- Weekly releases introduce breaking risk.
- The three valuable features can be replicated natively.

### Why not Agent Teams (alone)

- No resume = overnight dies on rate limit.
- No model routing = everything at Opus cost.
- Ephemeral = re-describe every session.

### Why custom automation

- Setup is 90% complete. Missing piece: orchestrator + resume script.
- Zero external dependencies.
- Full control over routing, artifacts, conventions.
- Debuggable with `cat` and `ls`.
- Model routing via YAML frontmatter.

---

## Implementation Architecture

### Agent Files (`.claude/agents/`)

**`.claude/agents/planner.md`** (the Architect):
```yaml
---
name: planner
description: Plans features, writes specs and implementation plans, conducts spec cross-checks, reviews Developer output. Use for any planning, feature design, architecture discussion, or review request.
model: opus
disallowedTools: Edit
---
```
Body = existing Architect.md content. `disallowedTools: Edit` prevents modifying source code. Write is allowed (needed for plan.md, spec.md, review.md).

**`.claude/agents/executor.md`** (the Developer):
```yaml
---
name: executor
description: Implements features from Architect plans step by step. Writes done.md and lessons.md on completion. Use for any implementation, coding, or build task.
model: opusplan
---
```
Body = existing Developer.md content.

**`.claude/agents/orchestrator.md`** (new):
```yaml
---
name: orchestrator
description: Coordinates the planning and implementation pipeline. Use when building a complete feature end-to-end or when asked to orchestrate.
model: opus
disallowedTools: Edit, Write
---
```
Body = orchestration logic:
1. Spawn planner → produces plan.md
2. Present plan for human approval (or skip if pre-approved for overnight)
3. Spawn executor → implements, produces done.md
4. Spawn planner for review Step 1 (done check)
5. If fails → spawn executor to fix → re-review
6. If passes → spawn planner for review Step 2 (code review)
7. If issues → spawn executor to fix → re-review
8. Max 3 fix cycles → escalate to human
9. Update lessons.md

All artifacts to `ai-coding/plans/{FeatureName}/`. Follow conventions in `ai-coding/conventions/agents-workflow.md`.

### Portability

Originals stay in `ai-coding/agents/` as documentation and portability reference for Codex CLI (AGENTS.md) and GitHub Copilot in VS (.agent.md). The `.claude/agents/` versions are what CC uses.

### Conductor instruction (root CLAUDE.md)

```markdown
## Agent Orchestration
When asked to build a feature end-to-end, delegate to the orchestrator agent.
When asked to plan or review, delegate to the planner agent.
When asked to implement, delegate to the executor agent.
For simple questions, quick fixes, or exploratory discussions — handle directly, do not delegate.
```

Selective delegation — substantial work only. Lighter than OMC's "delegate everything."

### Overnight Resume Script

```powershell
# resume-claude.ps1 — Windows Task Scheduler, every 30 minutes
$resumeFlag = "$env:USERPROFILE\.claude\auto-resume-enabled"
$logFile = "$env:USERPROFILE\.claude\session-log.txt"

if (-not (Test-Path $resumeFlag)) { exit }

$running = Get-Process -Name "claude" -ErrorAction SilentlyContinue
if ($running) { exit }

try {
    claude --continue --model opus -p "Check ai-coding/plans/ for in-progress features. Continue implementation or review pipeline. Report status."
    Add-Content $logFile "$(Get-Date) - Resumed successfully"
} catch {
    Add-Content $logFile "$(Get-Date) - Resume failed: $_"
}
```

Enable: `New-Item "$env:USERPROFILE\.claude\auto-resume-enabled" -Force`
Disable: `Remove-Item "$env:USERPROFILE\.claude\auto-resume-enabled"`

### Session tracking hook

`.claude/hooks.json`:
```json
{
  "hooks": {
    "Stop": [{
      "matcher": "",
      "command": "powershell -Command \"Add-Content '$env:USERPROFILE\\.claude\\session-log.txt' \\\"$(Get-Date) - Session stopped\\\"\""
    }]
  }
}
```

---

## Daily Workflows

### Daytime: Plan + supervised parallel implementation

1. Type: "Plan the {FeatureName} feature for the Submission service."
2. CC routes to planner (description matching + conductor instruction).
3. Planner asks batch questions, writes plan, cross-checks spec.
4. Review and approve plan.
5. For independent features across services, use Agent Teams:
   ```
   Create an agent team with two teammates:
   - "SubmissionDev" — implement ai-coding/plans/CreateArticle/plan.md
   - "AuthDev" — implement ai-coding/plans/Login/plan.md
   Both follow CLAUDE.md conventions. Write done.md when complete.
   ```
6. Monitor via `Shift+Down`, intervene as needed.

### Overnight: Sequential autonomous implementation

1. Review and approve pending plans.
2. Launch orchestrator:
   ```
   Use the orchestrator to implement these approved plans sequentially:
   1. ai-coding/plans/CreateArticle/plan.md
   2. ai-coding/plans/SubmitArticle/plan.md
   Run dotnet build after each step. Write done.md for each.
   If ambiguities exist, write them to done.md and skip.
   ```
3. Enable auto-resume.
4. Leave.
5. Morning: disable auto-resume, check done.md files, `dotnet build && dotnet test`, review.

---

## OMC Feature Replication

| OMC Feature | Custom Equivalent | Trade-off |
|---|---|---|
| `omc wait --start` | PowerShell scheduled task + `claude --continue` | Polls every 30 min vs exact reset time. Acceptable for overnight. |
| Model routing | YAML `model:` in frontmatter | Per-agent vs per-task. Matches actual workload. |
| Ralph mode | Orchestrator with 3-cycle cap | Capped is safer than unbounded. Protects budget and codebase. |
| HUD monitoring | `npx ccusage@latest` | Post-session vs real-time. Check in morning. |
| Cross-provider validation | Deferred | Could add Codex CLI as review agent later. |
| Team mode | CC Agent Teams (daytime) | Simpler but sufficient for supervised work. |

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Agents spawned with insufficient context | Agent files import conventions via `@`. CLAUDE.md auto-loads. Spawn prompts reference plan files explicitly. |
| Executor invents patterns overnight | CLAUDE.md: "Follow existing patterns. Never invent new ones. Write gaps to done.md." |
| Resume script creates duplicates | Check for running process. Flag file lifecycle. |
| Rate limits exceeded | Track with `ccusage daily`. Reduce overnight scope if needed. |
| Scope creep overnight | CLAUDE.md: "Do not modify outside plan scope." Git diff review in morning. |
| Subagent context limitations | Subagents have own 1M token window. Plan file is complete instruction set. |

---

## Key Principles

1. **Files are the contract, messages are ephemeral.** Plan files survive everything. Messages don't. Every important decision lands in a file.

2. **Humans approve plans, agents execute plans.** The approval boundary is where interactive becomes autonomous.

3. **Conventions are the investment, tools are adapters.** Conventions work across CC, Codex, Copilot. Tool-specific files are thin wrappers.

4. **Cap autonomous cycles, don't loop forever.** 3 fix cycles then escalate. Unbounded loops burn tokens and cascade bad decisions.

5. **Selective delegation, not universal.** Delegate substantial work. Handle simple questions directly. Saves tokens and latency.

6. **Zero external dependencies.** CC native features + PowerShell script. No plugins, no version pinning, no CLAUDE.md conflicts.

---

## Migration Steps

| Phase | When | What |
|---|---|---|
| 1 — Agent files | Day 1 | YAML frontmatter on Architect → `.claude/agents/planner.md`. Same for Developer → `executor.md`. Create `orchestrator.md`. Conductor instruction in CLAUDE.md. Test routing. |
| 2 — Resume script | Day 1-2 | Create `resume-claude.ps1`. Windows Scheduled Task. Hooks for state tracking. Test resume. |
| 3 — First overnight | Day 3-5 | Plan 1-2 features. Approve. Launch orchestrator overnight. Check morning results. Document lessons. |
| 4 — Iterate | Week 2 | Process lessons. Tune orchestrator. Add missing conventions. Evaluate Agent Teams for daytime parallel. |

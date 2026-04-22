# Stylization — Notes

**Source:** Solo session (2026-04-21). Recorded so the next session doesn't need the user to re-explain the context.

---

## Progress

### Foundation (1.1–1.5) — COMPLETE (2026-04-22)
- 1.1 Design tokens / theme — done (`@theme` block in `main.css`)
- 1.2 Shared component library — built (7 components in `components/ui/`), adoption happens during per-view work
- 1.3 AppHeader — done (logo, initials avatar, dropdown)
- 1.4 Page title — done ("Reflekt")
- 1.5 Scaffold cleanup — done (all scaffold files removed)

### Next: RetroRoomView (2.1–2.10)
Phase indicator, card redesign, card animations, column headers, vote button, join form, responsive layout, loading states, toasts, delete confirmation. ~10 items.

---

## What was discussed

Only in passing, as a flag for future work:

- A styling pass is needed across the Reflekt frontend.
- Route to the `designer` agent via the team pipeline.
- Sequence it **after** `PhaseActionGating` lands — the phase/action gating introduces new UI states (hidden/disabled controls, column visibility in RetroRoom) and it's cheaper to style on top of the final structure than to redo styling afterwards.

---

## What was NOT discussed

No specifics were agreed on. Explicitly open:

- Which views feel off (RetroRoom, Sprint, Report, Welcome, login, etc.).
- Whether the pain is polish (spacing, colors, typography), structural (layout, hierarchy, information density), or identity (brand, aesthetic, overall vibe).
- Scope: minor cleanup vs. a full visual redesign.
- Reference or inspiration — no target look was mentioned.
- Whether `shadcn-vue` should be pulled in (CLAUDE.md allows it "if it stays lightweight") or if this is a pure-Tailwind pass.

---

## What the next session needs before writing a plan

The user must state the problem first. One of:

1. **User articulates pain points.** One-liners per view / component are enough — the designer agent turns them into a plan.
2. **Designer agent does an audit pass** over the Vue views (`RetroRoomView`, `SprintView`, `ReportView`, `WelcomeView`, login, sprint list) and proposes a punch list for the user to triage. Only starts making changes after the user approves the list.

Do **not** start making visual changes without one of the above.

---

## Dependencies / sequencing

- **Blocked by:** `PhaseActionGating` merge. That feature changes the RetroRoom layout (conditional Action Items column, hidden/disabled controls per phase) and the SprintView phase display. Styling before it lands would be rework.
- **After PhaseActionGating merges:** any of the design-decisions.md "housekeeping" docs updates should also be done before styling, so the designer isn't working against stale specs.

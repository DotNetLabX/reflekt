# F40-ArchitectureDocs — Change Notes

Notes for shared files this unit does not edit; the campaign merge applies them.

## Note 1 — `docs/backlog.md`: historical note at the top

**Target:** `docs/backlog.md`.
**Source:** spec Scope, small-fixes row (AR-31); plan Step 12, Decision P2.
**Order:** any time at merge; independent of every other unit's notes.

**Change:** insert the text below as a new paragraph directly after the title line `# Architect Backlog — Reflekt v1` (line 1), before the paragraph that starts "Items deferred from initial alignment.", with one blank line before and after it. Nothing else in the file changes.

**Exact text:**

```
> **Historical — read as a record, not as current state (note added 2026-10-23).** Everything above item 23 except item 22 — the introduction with its "Already agreed" lines, and sections A–D (items 3–21) — records the v1 alignment and the plans of that time; it does not describe the app as built. Item 22 (phase revert) is still parked. Two kinds of statement there are superseded: the sign-in allowlist (items 6, 7 and 15) — no allowlist exists and any Google account may sign in (owner decision 2026-10-13; see item 35); and the aggregate split of "Already agreed" item 1 with `CardVote` as a value object inside `Card` (also items 10 and 11) — the app has seven aggregates, and `Vote` is one of them. The system as built is described in `docs/architecture/architecture.md` (one-page map: `docs/architecture/index.md`).
```

**Expected result:**
- `docs/backlog.md` opens (after its title) with the historical note; `git diff` on the file shows only the inserted lines.
- `grep -rniE "allowlist|AllowedEmails|allowlisted|not.invited" docs/backlog.md` → the note's own line (it states no allowlist exists), item 35's line (it states no allowlist is enforced), and the lines of items 6, 7 and 15, now covered by the note as historical and superseded.

**Owed at merge:** spec acceptance 2's `docs/backlog.md` part and acceptance 8's "`docs/backlog.md` opens with the historical note".

## Note 2 — merge order: F40's docs merge with or after F39's lint notes

**Target:** none — an ordering note for the campaign merge; no file text changes.
**Source:** plan Decision P4 (the docs describe the state after the sibling notes apply); F40 review, cycle 1 (second reader, open question 1).
**Order:** merge F40 **with or after** F39's change notes 1 (`web/reflekt-web/package.json` and `package-lock.json`: the ESLint and Prettier dev-dependencies and the `lint` script; itself after F37's notes on those files) and 2 (`.claude/verify.json`: the lint step in the `full` role).

**Change:** none in F40. These F40 lines describe the frontend `lint` script and the lint leg of `roles.full` as present, which holds only once F39's notes 1 and 2 are applied:
- `CLAUDE.md` — the Frontend line `npm --prefix web/reflekt-web run lint`;
- `docs/architecture/architecture.md` § 7 (the ESLint and Prettier dev-dependencies behind the `lint` script) and § 12 (**Lint**);
- `docs/conventions/project-rules.md` — Build Verification, `npm --prefix web/reflekt-web run lint`;
- `docs/conventions/testing.md` — frontend changes checked by `npm --prefix web/reflekt-web run lint`;
- `.claude/README.md` — `.claude/verify.json`'s `full` role described with the SPA's lint.

**Expected result:** after F39's notes 1 and 2 and F40 are merged, `npm --prefix web/reflekt-web run lint` exists and runs, and `.claude/verify.json`'s `full` role ends with the lint step; every line listed above is then true. Merged before them, those lines name a script and a role step that do not exist yet.

**Owed at merge:** the merge order above.

## Note 3 — `docs/backlog.md`: item 40 status at close

- **Target:** `docs/backlog.md`, item `### 40. Architecture Reference & Conventions Docs`.
- **Change:** replace `**Status: Spec Ready**` with `**Status: Done**` on that item's status line; keep the rest of the line.
- **Exact line after the change:** `**Status: Done** | **Spec:** \`docs/specs/F40-ArchitectureDocs/definition/spec.md\``
- Written by the team lead at F40's close (2026-10-23); the summary is `docs/specs/F40-ArchitectureDocs/delivery/summary.md`.

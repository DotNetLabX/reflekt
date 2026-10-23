# F39-FrontendConventions — Change Notes

Notes for shared files this unit does not edit; the campaign merge applies them.

## Note 1 — `web/reflekt-web/package.json`, `package-lock.json`: lint dev-dependencies and script

**Target:** `web/reflekt-web/package.json` and `web/reflekt-web/package-lock.json`.
**Source:** `questions.md` Q1 context and the campaign directive AR-1 (shared architect, 2026-10-23); plan Step 1, Decisions 1–3, P4; spec FE-37, Decisions row 3, acceptance 8.
**Order:** apply **after** F37's notes on these two files (F37 Note 2 runs `npm audit fix` first).

**Command** (from the repo root):

```
npm --prefix web/reflekt-web install -D eslint@10.13.0 eslint-plugin-vue@10.11.1 @vue/eslint-config-typescript@14.9.0 @vue/eslint-config-prettier@10.2.0 prettier@3.9.10
npm --prefix web/reflekt-web pkg set scripts.lint="eslint . && prettier --check src"
```

Before applying, re-check that `typescript-eslint` (pulled in by `@vue/eslint-config-typescript`, 8.72.0 at plan time, TypeScript peer `>=4.8.4 <6.1.0`) still accepts the TypeScript version F37 leaves in `package.json` (`~6.0.0` today). If it does not, record it and pick the nearest `@vue/eslint-config-typescript` whose `typescript-eslint` accepts it.

The configs these packages read are already committed by F39: `web/reflekt-web/eslint.config.js`, `web/reflekt-web/.prettierrc.json`, `web/reflekt-web/.prettierignore`.

**Expected result:**
- `package.json` gains the five `devDependencies` and the `lint` script; `package-lock.json` gains their tree; nothing else changes.
- `npm --prefix web/reflekt-web run lint` → 0 errors (warnings from the Vue `recommended` set do not count).
- `npm --prefix web/reflekt-web audit --omit=dev` → 0 vulnerabilities (F37's baseline holds; the new packages are dev-only).
- `npm --prefix web/reflekt-web run type-check` and `run build` stay green.

**Owed at merge:** spec acceptance 8's `run lint` and `audit --omit=dev`.

## Note 2 — `.claude/verify.json`: lint in the full role

**Target:** `.claude/verify.json`.
**Source:** spec Decisions row 3; plan Decision 1, P4.
**Order:** after Note 1 (the `lint` script must exist first, or every complete run fails).

**Change:** append ` && npm --prefix web/reflekt-web run lint` to `roles.full`, so it reads:

```
dotnet test Reflekt.slnx && npm --prefix web/reflekt-web ci && npm --prefix web/reflekt-web run build && npm --prefix web/reflekt-web run lint
```

Then re-time the full role and update `measured.full` (`ms`, `at`).

**Expected result:** the declared full role passes with the lint leg included.

## Note 3 — `docs/backlog.md`: remove the pre-F38 error reader after F38; merge order

**Target:** `docs/backlog.md`.
**Source:** `questions.md` Q1 answer (campaign shared architect AR-2, 2026-10-23); plan Step 4, Decision 4, P1; spec Dependencies and Scope row 1.
**Order:** any time at merge; the new row is actionable only once F38 is merged.

**Change:**

1. Add one backlog row: "Remove the pre-F38 error-body reader from web/reflekt-web/src/api/client.ts (F38 is merged; only F38's shape remains)". The reader is the function `readPreF38ErrorBody` in that file, marked by a comment pointing at this note; removing it leaves `readErrorBody` calling `readF38ErrorBody` alone.
2. On #39's row, record that #39 is built on F36 (not F38) and merges **before** #38. Until F38 is merged, `api/client.ts` reads both error bodies, so the create-team, create-sprint and delete-team forms show today's messages. When F38 merges second, its edit to `CreateTeamForm.vue` is dropped and F39's version wins (AR-2).

**Expected result:** the backlog holds the removal row, and #39's row states its merge order relative to #38.

## Note 4 — `docs/backlog.md`: #39 status at close

**Target:** `docs/backlog.md`, section `### 39. Frontend Conventions Conformance`.
**Source:** team-lead close, 2026-10-23 (`summary.md`).
**Order:** at merge, with Note 3.

**Change:** replace `**Status: Spec Ready**` with `**Status: Done**` and add `**Summary:** docs/specs/F39-FrontendConventions/delivery/summary.md`. Add one line under the row: "AC9 in-browser walkthrough (plan Step 16, Owner: operator) still owed; AC8 `run lint` and `audit --omit=dev` checked once Notes 1–2 are applied."

**Expected result:** #39 reads Done with its summary link and the two owed checks named.

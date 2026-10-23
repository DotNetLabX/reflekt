# F39-FrontendConventions — Lessons

## Architect Lessons

- **Compare the campaign's build order with the spec's Dependencies at Phase 1.** F39's spec depends on F38 (the new error body), but the campaign builds F39 on F36 only, so this tree still carries the pre-F38 error body and three SPA consumers parse it. A plan written to the spec's order alone would have shipped generic error text into `main` whenever F39 merges before F38. At Phase 1 of any campaign unit, read `campaign.json` `buildsOn` beside the spec's Dependencies and plan for the tree as built, not as ordered in the backlog.
- **Adding lint while the package files are frozen: prove the install path and mind `tsconfig.node.json`.** `npm install --no-save <pkgs>` left `package.json` and `package-lock.json` byte-identical (run at plan time), so configs can be committed and lint run before a change note adds the dev-dependencies. The Vue scaffold's `tsconfig.node.json` includes `eslint.config.*`: a TypeScript `eslint.config.ts` would break `vue-tsc --build` in every tree where the lint packages are not installed (any `npm ci` run), so the config has to be `eslint.config.js` until the dependencies land.

## Skill Gaps

### frontend-lint-setup
- **Kind:** missing
- **Searched for:** a skill that sets up ESLint and Prettier for the Vue SPA (flat config, Vue + TypeScript presets, Prettier skip-formatting, repo-specific rules such as no `fetch` outside `src/api`)
- **Why it would help:** F39 Step 1 had to describe the config inline; a skill would fix the preset choice, the JS-not-TS config rule under `tsconfig.node.json`, and the reformat-first order for every Vue repo
- **References:** web/reflekt-web/eslint.config.js, web/reflekt-web/.prettierrc.json (created by F39 Step 1), docs/specs/F39-FrontendConventions/delivery/plan.md § Step 1
- **Evidence:** [F39-FrontendConventions]

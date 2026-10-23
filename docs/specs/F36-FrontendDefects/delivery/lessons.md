# F36-FrontendDefects — Lessons

## Architect Lessons

- **A store-level "clear on fetch start" rule must be checked against every caller of that fetch, not only the views.** The F36 tech-spec (FE-08) says every fetch clears its target data when it starts; `cardGroup.fetchGroups` and `card.fetchCards` are also re-run by SignalR hub handlers (`useSprintHub.ts:67-71,108-120`) and after every group mutation (`cardGroup.ts:64,79,92,105,158`), so a literal clear would blank the board on every group change. The plan applies the rule per target (clear when the fetch is for a different sprint/team than the data held). When writing a state contract for a store, grep the fetch's callers (`grep -rn "\.fetchX("`) before fixing the wording.
- **A manual-walkthrough acceptance cannot be discharged by an "operator-owed, Deviated (valid reason)" fallback.** The F36 round-1 plan let unobserved AC6 items pass the done check as a justified deviation; the plan critic (R1-05, HIGH) rejected it because the spec asks for an observation per item. The fold that held: a separate `Owner: operator` step, with the acceptance kept open until it is recorded and the close reporting it as open. Use this shape for any acceptance that needs a human (OAuth sign-in, hearing audio), not the build-time-unavailable-resource fallback.
- **A "never another target's data" guarantee in a Vue SPA has to cover component reuse on a route-param change.** F36's views read their ids once at setup and load on mount, and `App.vue`'s `<RouterView>` is unkeyed, so Vue Router reuses the instance between `/sprints/5` and `/sprints/7` and no load runs; request counters cannot guard a request that never starts (plan review R2-02, missed in round 1). When planning load/stale-data contracts, check how each view reacts to its own route params (`watch` or a keyed `RouterView`) before relying on store-level guards.

## Skill Gaps

### tailwind-theme — White Flash Prevention
- **Kind:** ill-fitting
- **Searched for:** a flash-free first paint that also works in the light theme
- **Why it would help:** `.claude/skills/tailwind-theme/SKILL.md` § White Flash Prevention prescribes `<body style="background-color: #0a0a0f;">`, which is the F34 FE-05 defect (the inline style beats the stylesheet, so the light theme keeps a dark page). F36 Step 2 replaces it with a `<head>` `<style>` (`html` / `html.light` backgrounds). The skill is not edited (owner decision 2026-10-12); `docs/conventions/vue.md` § White Flash Prevention carries the same wording and is F40's.
- **References:** `.claude/skills/tailwind-theme/SKILL.md` (§ White Flash Prevention), `web/reflekt-web/index.html`, `docs/conventions/vue.md`
- **Evidence:** [F34-ConventionsAudit, F36-FrontendDefects]

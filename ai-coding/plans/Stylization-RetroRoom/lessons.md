# RetroRoom Stylization — Lessons

## Planner Lessons
- Plan Step 3 said "Replace raw text inputs with BaseInput" but the edit textarea is a `<textarea>` which BaseInput doesn't wrap. Plans should distinguish `<input>` vs `<textarea>` when specifying component adoption.
- Plan Step 5 specified "brief scale pulse via transition: transform 0.15s" but a CSS transition requires an actual transform state change to animate — just changing `:key` remounts the element without triggering a transition. Plans should specify the actual animation mechanism (e.g., `@keyframes` with a scale bounce) rather than assuming transition properties will fire on element creation.
- When multiple plan steps modify the same file, explicitly noting "these steps will be combined into a single coordinated edit" in the plan saves the executor from navigating this themselves.

## Executor Lessons
- `maxlength` on Vue components must use bound syntax (`:maxlength="N"`) not string attributes (`maxlength="N"`) — Vue passes unbound attributes as strings, causing TypeScript type errors against `number` props.
- Module-level reactive state in composables (outside the function body) creates singletons — correct for app-wide toast state but would be wrong for per-instance state.
- `position: absolute` requires `relative` on the nearest ancestor you want it positioned against — easy to miss when the absolute element is inside a TransitionGroup or iteration.

## Reviewer Lessons
- Pre-commitment predictions caught 4/4 actual findings — the technique is effective for Vue component work.
- Cross-validation between Sonnet and Codex reached full agreement on findings; severity disagreements were resolved by build evidence (type errors blocking CI = HIGH, not MEDIUM).
- The `BaseInput` wraps `<input>` not `<textarea>` gap should be flagged during plan review (Step 1), not code review (Step 2).

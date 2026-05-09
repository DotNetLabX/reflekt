# Skill Audit Fixes — Lessons

## Architect Lessons

- Skills were written for a larger project template (with Blocks.Exceptions, Blocks.AspNetCore, Blocks.Core, AuditedEntityConfiguration) that doesn't match the current Reflekt BuildingBlocks. Skill definitions need a "prerequisites" section listing which BuildingBlocks packages they depend on, so the audit can detect missing infrastructure before planning.

- The skill audit should classify findings in two directions: "code deviates from skill" (code fix) vs "skill deviates from code" (skill fix). Mixing these in the same severity rating is confusing — a MEDIUM finding that means "update the skill" is very different from one that means "refactor the code."

- Entity config naming (`{Name}Configuration` vs `{Name}EntityConfiguration`) and base class (`EntityConfiguration<T>` vs `AuditedEntityConfiguration<T>`) were skill-vs-code mismatches that existed since the first aggregate was created. Skills should be validated against the codebase immediately after initial implementation, not months later.

- The error-handling skill documents a GlobalExceptionMiddleware and exception hierarchy that were never implemented in Reflekt. The plan initially assumed they existed and prescribed removing try/catches — which would have introduced 500 errors. Always verify that referenced infrastructure exists before writing fix steps.

- When the audit finds missing BuildingBlocks infrastructure, that's a separate plan — don't inline infrastructure creation into an audit fix plan. Infrastructure changes have their own scope, testing, and risk profile.

- Plan naming for extracted components should be verified against the actual view content, not assumed from the view name. "SprintView" sounds like a list but is a detail page — the plan named the extraction "SprintList" which the developer correctly deviated from. Cross-check view content before naming extraction targets.

- When a plan prescribes splitting N identical components into N separate files, add a note that consolidation into 1 component is acceptable IF the structure is truly identical and differentiation is policy-driven. The 4-phase-board-to-1 consolidation was the right call but required 3 review cycles to get the resulting file under the line limit. Anticipating the line-count pressure at plan time would have prescribed the composable extraction upfront.

- Shell components (PageLayout, PageToolbar) need their layout model documented in the plan — specifically which views are compatible and which are not. The RetroRoomView incompatibility (viewport-height vs min-h-screen) was discovered during implementation. The plan should have flagged this as a known exception.

- A 10-step plan with 3 phases (skills, backend, frontend) and ~70 files touched took 3 review cycles. The frontend decomposition phase (Steps 7-10) drove all the review friction. For future audit-fix plans, consider splitting backend and frontend into separate plans so each can complete its review cycle independently.

## Developer Lessons

- Vue's `v-if`/`v-else-if` branches inside `<TransitionGroup>` (and slot content rendered by TransitionGroup) must have **unique `key` values** even between branches. Two sibling `v-if`/`v-else-if` divs with the same `key="empty"` fail at build time with "v-if/else branches must use unique keys." Use `key="empty-groups"` / `key="empty-cards"` to disambiguate.

- When a plan names an extracted component (e.g., "SprintList") but the view being decomposed doesn't actually contain a list, check the actual view content before creating the component. The plan had `SprintView.vue` → `SprintList.vue` but `SprintView.vue` is a detail page. Created `SprintDetailPanel.vue` instead and noted the deviation.

- L1 phase boards that are structurally identical should be consolidated into one board component driven by policy store, not split into 4 separate files. Splitting identical structure multiplies maintenance cost with no comprehension benefit. The skill's "MUST split" rule addresses when to split, but the inverse (don't split identical structure) is equally important.

- `PageLayout` with `min-h-screen` is incompatible with views that need `h-[calc(100vh-3.5rem)] overflow-hidden` (e.g., the retro room's full-height board layout). Shell components need to document their layout model clearly so developers can decide when not to use them.

- When replacing a widely-used component (CardItem/CardGroupItem) with a new one (RetroCardItem/RetroGroupItem), keep the old files on disk until reviewer sign-off rather than immediately deleting — they may be needed as reference during review.

- When most of the decomposition already exists (components already extracted), the remaining work is wiring views to use those components. Always grep for existing sub-components before creating new ones — saves significant work.

- `PageLayout`'s `empty` slot pattern needs care: the `empty` prop should be `!loading && !data` not just `!data`, otherwise the empty state flashes before the first fetch completes.

- An L1 "TeamSelector" is the right level for WelcomeView's orchestration because it needs three-store access and watch-based data loading. A view that is purely a pass-through to one L1 component (WelcomeView → TeamSelector) is valid — the view stays as L0 route target even if it has no logic of its own.

- A thin L2 pass-through wrapper (e.g., `RetroColumnBody`) is the correct way to eliminate repeated `v-for`-driven slot blocks that exceed 20 lines each. The wrapper re-exposes slots verbatim and forwards props with matching types — the parent's template shrinks without any logic moving.

- When a composable owns `onMounted`/`onUnmounted`, the consuming view must NOT also import those lifecycle hooks for the same concern. The composable's hooks fire in the correct component context because it is called inside `<script setup>` — no extra wiring needed in the view.

- `RetroColumnPanel`'s `badgeVariant` prop is a union type (`'success' | 'danger' | 'info' | 'primary'`), not `string`. Any wrapper component that forwards this prop must replicate the same union — using `string` causes a TS2322 type error at `vue-tsc` time even though Vite's Rollup build passes.

- Mapster IRegister conversion: multi-param mapping methods (e.g., `CardGroupMappings.ToResponse(group, cards, voteCount, isFocused)`) become `group.Adapt<Response>() with { Cards = ..., VoteCount = ... }`. Adding a `Card → CardGroupCardEntry` config to the IRegister simplified nested collection mapping at call sites.

- `required init` properties work with private parameterless constructors without `[SetsRequiredMembers]` — EF Core sets them via reflection. Follow the Team/Sprint pattern already in the codebase.

- The initial grep for `.ToResponse(` missed 3 files (GetParticipantsEndpoint, GetTeamSprintsEndpoint, GetParticipantReportEndpoint) because they used method reference syntax (`Select(ParticipantMappings.ToResponse)`) instead of call syntax. Always build after removing the old static classes to catch stragglers.

## Skill Gaps

- **Missing skill:** No skill covers Vue component decomposition execution — what files to create, how to wire slots, how to structure the L0/L1/L2/L3 hierarchy in practice. The `vue-component-architecture` skill covers the rules but not the workflow steps. Suggested name: `decompose-vue-component`. Coverage: read existing component, identify extraction points, create component files in correct directories, update parent imports, verify build.

## Reviewer Lessons

- When a plan consolidates N identical components into 1 (e.g., 4 phase boards → 1 RetroBoardPanel), the resulting component still must satisfy the skill's line-count limit. Consolidation is not a bypass — it just moves the split obligation. Check the line count of every consolidated component regardless of how well-justified the consolidation decision was.

- Check `window.addEventListener` calls for matching `removeEventListener` in `onUnmounted`. Components that subscribe to global DOM events without cleanup leak listeners on every mount/unmount cycle. This is a HIGH issue, not LOW — it degrades performance on repeated navigation.

- Skill template examples that use the wrong API (wrong props, wrong slot names) are MEDIUM findings, not LOW. A developer following a template example verbatim will write TypeScript-invalid code. Always verify that skill code examples match the actual component interface, especially after a component is refactored.

- For Mapster IRegister reviews: check both that enum configs are present AND that multi-param call sites use `with` expressions correctly. The `VoteCount = 0` default registered in the config creates a silent failure mode if a call site forgets the `with { VoteCount = voteCount }`. Flag this as a gap even when no current call site is incorrect.

- L3 components with local `ref` state are a MEDIUM (not HIGH) violation when the state is purely UI-local (editing mode, link input toggle) and the component has no store coupling. The skill says L3 = "Forbidden" for composables but inline `ref` usage in `<script setup>` is a judgment call — escalate to MEDIUM rather than HIGH unless the state should clearly be in a store or parent.

- Pre-commitment predictions are valuable: the three backend predictions (missing enum configs, surviving `.ToResponse()` calls, EF Core hydration break) were all checked systematically and all came up clean. The frontend predictions were less specific — next time predict specific component line counts based on the implementation report's description of what was extracted.

- Always verify claimed line counts with a fresh `wc -l` — never trust implementation.md's "Result: N lines" without independent measurement. In cycle 2, the developer claimed 240 lines for `RetroBoardPanel.vue` but fresh measurement returned 351. The discrepancy (111 lines) was large enough to keep the HIGH finding open for another cycle.

- When a developer extracts a composable to reduce a view's line count, also verify the composable's own line count. `useRetroRoom.ts` is 112 lines — within L1 range, acceptable for a composable, but worth noting for future composable extraction plans that composables don't have a hard line limit but should stay focused.

- A composable that accepts an `onError` callback parameter rather than owning its own `useToast` call is the correct pattern for logic-only composables. It keeps the composable free of UI side effects and lets the consumer decide how to surface errors. When reviewing composable extractions, verify this separation: no `useToast`, no DOM refs, no `onMounted`/`onUnmounted` unless the composable explicitly owns a lifecycle resource (e.g., `useRetroRoom` owns the SignalR connection).

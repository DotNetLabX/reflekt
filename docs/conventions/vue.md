# Vue Conventions

Programming conventions for Vue 3 + TypeScript specific to this codebase. Referenced by developer and reviewer agents. Complements the skills in `.claude/skills/` (vue-patterns, vue-component-architecture, pinia-patterns, tailwind-theme).

## Reactivity Gotchas

- When a composable is called in multiple places, declare shared reactive refs at **module level** (outside the exported function). Function-scoped refs give each call site an isolated copy. `useToast` keeps `const toasts = ref<Toast[]>([])` and `useTheme` its `theme` ref at module level, so every caller sees the same list and theme.
- Guard against empty-state flickering: show the empty state only once loading has finished and there is no error. `SprintList.vue` checks `error` first, then `!loading && sprints.length === 0`. Without the `!loading` guard, "No sprints yet" flashes while the list loads.
- When multiple code paths write the same state, apply the **same rule** on every path. In the card store, an edit's API response (`editCard`) and the `CardUpdated` hub message (`applyCardUpdated`) both write a card's content, and both go through `pendingEdits` (confirm or settle), so neither overwrites a newer change with an older value.

## Component Patterns

- `RouterLink` is globally registered by Vue Router — no import needed in `<script setup>` (`AppHeader.vue` uses it without one).
- Use `withDefaults(defineProps<T>(), { ... })` for components with optional props that have defaults — `CreateTeamForm.vue`: `withDefaults(defineProps<{ heading?: string; showCancel?: boolean }>(), { heading: 'Create a new team' })`.
- Native `<select>` styling differs per browser: set the background and border from the theme tokens. The assignee pickers in `RetroActionItemInput.vue` and `RetroCardItem.vue` use `bg-surface`, `border-border` and `text-text-primary`; a select that must drop the native arrow too also needs `appearance-none`.

## Store / View Boundary

- Store actions only mutate store-owned state. View handlers mutate form/UI state. Never cross the boundary.
- Error state from the store (`error` ref) must be surfaced in views — not silently swallowed (`SprintList.vue` shows the sprint list's `error` with a Retry button).
- Empty state must be rendered when data or filter produces zero results — `RetroBoardPanel.vue` shows "No cards yet" (`column.cards.length === 0`) or "No groups yet" in an empty column instead of rendering nothing.

## White Flash Prevention

- `index.html` sets the page background in a `<style>` block in `<head>`, with literal hex values: `html { background: #0a0a0f; }` (dark, the default) and `html.light { background: #f4f4f8; }`. Tailwind CSS custom property tokens are not available until the CSS file loads, so this paints the right colour before first paint.
- The inline script after it reads the saved theme (`reflekt:theme`) and adds the `light` class to `<html>` before the app loads, so the light background applies at once.
- `<body>` carries no colour of its own; once the stylesheet loads, `body` takes `var(--color-background)`. Keep both hex values in step with the theme tokens.

## TypeScript

- `erasableSyntaxOnly` (Vite default) forbids `enum` declarations. Use type unions: `type SprintState = 'Draft' | 'Active' | 'Closed'` (`src/types/domain.ts`).

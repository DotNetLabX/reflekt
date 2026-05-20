# Dark / Light Theme

**Feature Spec:** `docs/specs/F31-DarkLightTheme/definition/spec.md`

## Context

Reflekt's interface is currently light-only with hardcoded Tailwind gray/white classes across all components. This feature introduces a dark/light theme system with dark as the default, using CSS custom properties and a class-based toggle. The change is purely frontend -- no backend modifications. It touches the CSS token layer, the HTML shell, a new composable, and every Vue component that uses hardcoded color classes (38 files, ~230 gray/white occurrences, ~48 semantic color occurrences).

## Scope

**In scope:**
- Two-layer CSS token system (`:root` = dark default, `.light` class override)
- Theme toggle composable (`useTheme`) with localStorage persistence
- Flash-prevention inline script in `index.html`
- Sun/moon toggle button in `AppHeader.vue`
- Migration of all hardcoded Tailwind color classes to token-based classes across all components
- Theme-aware column `-light` variant tokens (low-opacity in dark, current hex in light)
- Theme-aware semantic badge/status colors
- Theme-aware shadows (suppressed for cards in dark, preserved in light; modal shadows kept in both)
- Dark-mode scrollbar styling
- Focus ring offset color matching page background per theme
- Tooltip wiring from `help.tooltips.md`

**Explicitly out of scope:**
- Per-user server-side theme preference
- System preference detection (`prefers-color-scheme`)
- Theme transition animation
- Per-component theme override
- Custom accent color selection
- High contrast mode

## Skill Mapping

| Step | Skill | Disposition | Feature-Specific Inputs | Gap? |
|------|-------|-------------|------------------------|------|
| 1 | `tailwind-theme` | Follow | Spec palette values, token names from spec Token Mapping table, two-layer pattern with `:root`=dark default | |
| 2 | `vue-patterns` | Follow | `useTheme` composable shape, localStorage key `reflekt:theme`, dark default | |
| 3 | `tailwind-theme` | Follow | Flash-prevention inline script pattern, `<body>` inline background-color | |
| 4 | `vue-component-architecture` | Follow | Toggle button placement in AppHeader (L1/L2), tooltip from help.tooltips.md | |
| 5 | (none) | -- | UI primitive migration: token class replacements per component | Gap: no "theme-migrate" skill |
| 6 | (none) | -- | Feature component migration: token class replacements per component | Gap: same |
| 7 | (none) | -- | View + remaining component migration: token class replacements | Gap: same |
| 8 | (none) | -- | Verify build passes, visual check both themes | |

## Domain Model Changes

None. This feature is purely frontend.

## Data Model Changes

None. Theme preference is stored in browser localStorage, not the database.

## Implementation Steps

### Step 1: Rewrite CSS token system

**Skill:** Follow `tailwind-theme`

**What:** Rewrite `web/reflekt-web/src/assets/main.css` to use the two-layer token pattern. `:root` defines dark theme values (the default). `.light` class on `<html>` overrides to light values.

**Files to modify:**
- `web/reflekt-web/src/assets/main.css`

**Feature-specific inputs:**

New CSS custom properties to add (from spec Token Mapping):
- `--color-border` (dark: `#2a2a38`, light: `#e2e2ea`)
- `--color-border-subtle` (dark: `#1e1e2a`, light: `#ededf3`)
- `--color-text-primary` (dark: `#f0f0f5`, light: `#1a1a2e`)
- `--color-text-secondary` (dark: `#a0a0b8`, light: `#6b6b80`)
- `--color-text-muted` (dark: `#60607a`, light: `#9b9bab`)
- `--color-scrollbar-thumb` (dark: `#2a2a38`, light: browser default)
- `--color-focus-offset` (dark: `#0a0a0f`, light: `#f4f4f8` -- matches page background)

Existing properties to make theme-aware:
- `--color-background` (dark: `#0a0a0f`, light: `#f4f4f8`)
- `--color-surface` (dark: `#13131a`, light: `#ffffff`)
- `--color-surface-raised` (dark: `#1c1c26`, light: `#eaeaf0`)

Shadow tokens to make theme-aware:
- `--shadow-card` (dark: `none`, light: current value `0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06)`)
- `--shadow-card-hover` (dark: `none`, light: current value `0 4px 12px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06)`)
- `--shadow-modal` stays the same in both themes

Column `-light` tokens to make theme-aware (all 8 palette colors + action-items):
- Dark mode: the saturated color at ~10% opacity (e.g., `--color-col-green-light` dark = `rgba(16,185,129,0.10)`)
- Light mode: current hex values (e.g., `--color-col-green-light` light = `#d1fae5`)

The `@theme` block maps Tailwind utility names to CSS custom properties. The `@source inline(...)` directive for column color class safelist stays.

Primary palette (`--color-primary-*`) stays unchanged -- not theme-dependent per spec.

Semantic badge tokens (6 variants x 2 properties = 12 tokens, from spec Semantic Color Variants table):

| Token | Dark value | Light value |
|-------|-----------|-------------|
| `--color-badge-default-bg` | `rgba(96,96,122,0.10)` | `#f1f5f9` |
| `--color-badge-default-text` | `#a0a0b8` | `#64748b` |
| `--color-badge-success-bg` | `rgba(16,185,129,0.10)` | `#d1fae5` |
| `--color-badge-success-text` | `#34d399` | `#047857` |
| `--color-badge-danger-bg` | `rgba(244,63,94,0.10)` | `#ffe4e6` |
| `--color-badge-danger-text` | `#fb7185` | `#be123c` |
| `--color-badge-warning-bg` | `rgba(245,158,11,0.10)` | `#fef3c7` |
| `--color-badge-warning-text` | `#fbbf24` | `#92400e` |
| `--color-badge-info-bg` | `rgba(59,130,246,0.10)` | `#dbeafe` |
| `--color-badge-info-text` | `#60a5fa` | `#1d4ed8` |
| `--color-badge-primary-bg` | `rgba(99,102,241,0.10)` | `#e0e7ff` |
| `--color-badge-primary-text` | `#818cf8` | `#4338ca` |

These must be defined in the `@theme` block (so Tailwind generates `bg-badge-success-bg`, `text-badge-success-text`, etc.) and assigned values in `:root` (dark) / `.light` blocks. Step 5 uses these tokens directly in BaseBadge variant classes.

**Acceptance criteria:**
- `:root` block produces a dark-themed page by default
- `.light` class produces the current light appearance
- All token values match the spec palette tables exactly
- Column `-light` tokens resolve to low-opacity tints in dark, solid hex in light
- Shadows suppressed on cards in dark, preserved in light
- `body` font-family and `bg-background` continue to work

### Step 2: Create theme composable

**Skill:** Follow `vue-patterns`

**What:** Create `useTheme` composable that reads/writes theme preference to localStorage and toggles the class on `<html>`.

**Files to create:**
- `web/reflekt-web/src/composables/useTheme.ts`

**Feature-specific inputs:**
- localStorage key: `reflekt:theme`
- Default value: `'dark'` (if no stored preference)
- Exported: `theme` (ref, `'dark' | 'light'`), `isDark` (computed boolean), `toggleTheme` (function)
- On toggle: set `document.documentElement.className` to the new theme value, persist to localStorage
- The composable uses module-level refs so all consumers share the same state

**Acceptance criteria:**
- `toggleTheme()` switches between dark and light
- Theme class is set on `<html>` element
- Preference persists to localStorage under `reflekt:theme`
- Multiple components importing `useTheme` share the same reactive state

### Step 3: Flash-prevention and HTML shell

**Skill:** Follow `tailwind-theme`

**What:** Add inline blocking script to `index.html` that reads localStorage and sets the theme class before first paint. Add inline `style` on `<body>` for dark background.

**Files to modify:**
- `web/reflekt-web/index.html`

**Feature-specific inputs:**
- `<body style="background-color: #0a0a0f;">` -- dark default prevents white flash before any JS or CSS runs.
- Inline `<script>` at the **end of `<body>`** (after `<div id="app">`, before the Vite module script). Placing it here means `document.body` exists and can be manipulated:
  - Reads `localStorage.getItem('reflekt:theme')`
  - Sets `document.documentElement.className` to `'light'` or `'dark'` accordingly (default: `'dark'`)
  - Removes the inline background-color: `document.body.style.backgroundColor = ''` -- this lets the CSS rule `body { background-color: var(--color-background); }` take over immediately, using the correct theme value since the class is already set on `<html>`.
- Add `class="dark"` to `<html>` as the static default (script overrides before paint)
- The sequence is: browser paints `<body>` with inline dark bg -> script runs, sets theme class, clears inline style -> CSS variables resolve per theme class -> correct background applied. No flash because the `<html>` class and CSS are resolved before the Vite app mounts.

**Acceptance criteria:**
- No flash of wrong theme on page load (spec business rule 5)
- First visit renders dark (no localStorage entry)
- Returning visit with `reflekt:theme=light` renders light immediately

### Step 4: Theme toggle button in AppHeader

**Skill:** Follow `vue-component-architecture`

**What:** Add a sun/moon icon toggle button in the AppHeader, positioned to the left of the user avatar button. Import and use the `useTheme` composable.

**Files to modify:**
- `web/reflekt-web/src/components/AppHeader.vue`

**Feature-specific inputs:**
- Button placement: in the right-side `div`, before the user avatar button. Visible on all pages (the header already appears on all authenticated pages).
- Icons: `Sun` and `Moon` from `lucide-vue-next`
- Dark mode shows Sun icon (action: switch to light). Light mode shows Moon icon (action: switch to dark).
- Button styling: icon-only, same size as avatar button area (`w-9 h-9`), uses token classes for hover state.
- Tooltip text from `help.tooltips.md`: "Switch between dark and light appearance."
- Also migrate all hardcoded color classes in this component to tokens (this component has 8 occurrences of gray/white classes).

**Acceptance criteria:**
- Toggle button visible on all pages with the header
- Clicking toggles the theme instantly
- Icon reflects the action (sun in dark, moon in light)
- Tooltip matches help.tooltips.md

### Step 5: Migrate UI primitive components to theme tokens

**Skill:** None (mechanical class replacement -- no skill covers bulk migration)

**What:** Replace all hardcoded Tailwind gray/white/semantic color classes with token-based classes in the base UI components. These are the foundation -- all other components build on them.

**Files to modify (8 files):**
- `web/reflekt-web/src/components/ui/BaseButton.vue` -- `bg-white`, `text-gray-700`, `border-gray-300`, `bg-gray-50`, `bg-gray-100`, `bg-gray-200`, `focus-visible:ring-offset-2` (needs `ring-offset-[--color-focus-offset]`)
- `web/reflekt-web/src/components/ui/BaseInput.vue` -- `bg-gray-50`, `text-gray-400`, `border-gray-200`, `border-gray-300`, `text-gray-900`, `bg-white`
- `web/reflekt-web/src/components/ui/BaseCard.vue` -- `border-gray-200` (3 occurrences in header/footer/body borders)
- `web/reflekt-web/src/components/ui/BaseModal.vue` -- `border-gray-200`, `text-gray-900`, `text-gray-400`, `text-gray-600`
- `web/reflekt-web/src/components/ui/BaseBadge.vue` -- all variant classes (`bg-gray-100 text-gray-700`, `bg-emerald-50 text-emerald-700`, etc.)
- `web/reflekt-web/src/components/ui/EmptyState.vue` -- `text-gray-300`, `text-gray-500`, `text-gray-400`
- `web/reflekt-web/src/components/ui/LoadingSpinner.vue` -- `text-gray-500`
- `web/reflekt-web/src/components/ui/ToastContainer.vue` -- `border-gray-200`, `text-gray-800`, `text-gray-400`, `text-gray-600`

**Token replacement mapping (apply consistently across all files):**

| Hardcoded class | Token class |
|----------------|-------------|
| `bg-white` | `bg-surface` |
| `bg-gray-50` | `bg-surface-raised` |
| `bg-gray-100` | `bg-surface-raised` |
| `bg-gray-200` | `bg-border` (for button hover states where gray-200 is a hover fill) |
| `text-gray-900` / `text-gray-800` | `text-text-primary` |
| `text-gray-700` | `text-text-primary` (when used for primary content) or `text-text-secondary` (when used for labels) |
| `text-gray-600` | `text-text-secondary` |
| `text-gray-500` | `text-text-secondary` |
| `text-gray-400` | `text-text-muted` |
| `text-gray-300` | `text-text-muted` |
| `border-gray-200` / `border-gray-300` | `border-border` |
| `border-gray-100` | `border-border-subtle` |
| `hover:bg-gray-50` | `hover:bg-surface-raised` |
| `hover:bg-gray-100` | `hover:bg-surface-raised` |
| `hover:text-gray-600` / `hover:text-gray-700` | `hover:text-text-secondary` |
| `focus-visible:ring-offset-2` | `focus-visible:ring-offset-2 ring-offset-[--color-focus-offset]` |

**BaseBadge semantic variants** -- replace with the badge tokens defined in Step 1:
- `default`: `bg-badge-default-bg text-badge-default-text`
- `success`: `bg-badge-success-bg text-badge-success-text`
- `danger`: `bg-badge-danger-bg text-badge-danger-text`
- `warning`: `bg-badge-warning-bg text-badge-warning-text`
- `info`: `bg-badge-info-bg text-badge-info-text`
- `primary`: `bg-badge-primary-bg text-badge-primary-text`

**Pattern reference:** Current `BaseButton.vue` for variant class map structure.

**Acceptance criteria:**
- All 8 UI primitive files use only token-based color classes (no `gray-*`, no `bg-white`, no hardcoded semantic colors)
- Components render correctly in both themes
- Focus rings have no white gap on dark backgrounds
- Badge variants are readable and distinct in both themes

### Step 6: Migrate feature components to theme tokens

**Skill:** None (mechanical class replacement)

**What:** Replace hardcoded color classes in all feature-area components. Group by feature area.

**Files to modify (22 files, 1 verified clean):**

**Teams (4 files):**
- `web/reflekt-web/src/components/teams/TeamCard.vue` -- `bg-white`, `border-gray-200`, `text-gray-800`, `text-gray-400`, `text-gray-500`, `hover:bg-gray-100`, `bg-gray-800` (tooltip bg -- keep as-is, tooltips use dark bg in both themes for contrast), `border-gray-800` (swatch selection border -- use `border-text-primary` for contrast in both themes)
- `web/reflekt-web/src/components/teams/CreateTeamForm.vue` -- `bg-white`, `text-gray-800`, `border-gray-300`, `text-gray-600`, `bg-blue-600` (use `bg-primary-600`), `hover:bg-gray-50`, `border-gray-300`
- `web/reflekt-web/src/components/teams/DeleteTeamDialog.vue` -- `bg-white`, `text-gray-800`, `text-gray-600`, `text-gray-700`, `border-gray-300`, `bg-gray-100`, `hover:bg-gray-200`, `bg-red-600`, `hover:bg-gray-100`
- `web/reflekt-web/src/components/teams/TeamSelector.vue` -- `text-gray-800`, `text-gray-500`, `bg-blue-600`

**Sprints (3 files):**
- `web/reflekt-web/src/components/sprints/SprintList.vue` -- `bg-white`, `text-gray-800`, `text-gray-400`, `border-gray-200`, `hover:bg-gray-50`, `text-gray-500`, `bg-gray-100`/`bg-green-100`/`bg-red-100` (status badges -- use semantic badge tokens)
- `web/reflekt-web/src/components/sprints/SprintDetailPanel.vue` -- `bg-white`, `text-gray-800`, `text-gray-600`, `text-gray-500`, `bg-blue-600`, `bg-green-600`, `bg-gray-200`, `bg-gray-50`, `text-red-600`, `bg-gray-100`/`bg-green-100`/`bg-red-100` (status badges)
- `web/reflekt-web/src/components/sprints/CreateSprintForm.vue` -- `border-gray-100`, `text-gray-700`, `border-gray-300`, `text-gray-500`, `text-gray-600`, `bg-blue-600`, `focus:ring-blue-500` (use `focus:ring-primary-500`), `border-gray-800` (swatch selected -- use contrasting border), `text-red-600`

**Retro (12 files to modify, 1 verified clean):**
- `web/reflekt-web/src/components/retro/RetroColumnPanel.vue` -- `bg-white` (inline edit input), `border-gray-300`, `border-gray-800` (swatch selection), `bg-primary-50/30` (drag-over -- replace with a theme-aware drag-over token or `bg-primary-500/10`)
- `web/reflekt-web/src/components/retro/RetroCardItem.vue` -- `text-gray-800`, `text-gray-400`, `text-gray-600`, `border-gray-300`, `text-gray-300`, `bg-white` (card bg class fallback). Native `<textarea>` (line 17) and `<select>` (line 18) need explicit `bg-surface text-text-primary border-border` classes for dark mode visibility.
- `web/reflekt-web/src/components/retro/RetroGroupItem.vue` -- `text-gray-800`, `text-gray-300`, `text-gray-500`, `border-gray-200`, `text-gray-600`, `border-gray-300`, `text-gray-700`, `bg-white` (inline edit), `bg-primary-50` (focused group -- use `bg-primary-500/10`). Inline edit `<input>` (line 32) needs `bg-surface text-text-primary border-border`. Also fix pre-existing bug: `groupBgClass` uses `bg-${props.columnColor}-light/30` but should be `bg-col-${props.columnColor}-light/30` (missing `col-` prefix).
- `web/reflekt-web/src/components/retro/RetroGroupDiscussion.vue` -- `text-gray-500`, `hover:text-gray-700`, `text-green-600` (discussed indicator -- use `text-success` for green, token classes for gray)
- `web/reflekt-web/src/components/retro/RetroReportPanel.vue` -- `text-gray-800`, `text-gray-500`, `text-gray-700`, `text-gray-400`, `border-col-{color}-light` (already token-based, good), evidence link classes already use column tokens
- `web/reflekt-web/src/components/retro/RetroBoardPanel.vue` -- `text-gray-400`, `border-gray-300`, `text-gray-600`, `bg-emerald-600`, `bg-white`. Native `<select>` (line 164) needs `bg-surface text-text-primary border-border` for dark mode visibility.
- `web/reflekt-web/src/components/retro/RetroPhaseHeader.vue` -- `text-gray-800`, `bg-gray-900` (SM tooltip bg -- keep dark), `text-gray-300`, `text-emerald-300`
- `web/reflekt-web/src/components/retro/RetroJoinForm.vue` -- `text-gray-800`, `border-gray-300`, `text-gray-700`, `bg-white`, `hover:bg-gray-50`, `text-gray-400`, `bg-gray-200`
- `web/reflekt-web/src/components/retro/RetroMobileTabBar.vue` -- no hardcoded grays (uses column color tokens already, good)
- `web/reflekt-web/src/components/retro/PreviousActionsPanel.vue` -- `bg-white`, `border-amber-200`, `bg-amber-50`, `text-amber-800`, `text-amber-600`, `text-gray-600`, `text-gray-800`, `text-gray-500`, `divide-gray-100`, `bg-emerald-600`, `bg-amber-500`, `bg-gray-500`, `bg-white`, `border-gray-300`, `hover:bg-gray-50`, `bg-emerald-100`, `bg-amber-100`, `bg-gray-100`
- `web/reflekt-web/src/components/retro/RetroParticipantList.vue` -- `text-gray-500` (section heading) -> `text-text-secondary`
- `web/reflekt-web/src/components/retro/TimerDisplay.vue` -- `text-gray-800`, `text-gray-500`, `text-red-600`, `text-gray-400`, `hover:bg-gray-100`
- `web/reflekt-web/src/components/retro/TimerControls.vue` -- `bg-white`, `border-gray-200`, `bg-gray-100`, `hover:bg-gray-200`, `text-gray-700` (in `<style scoped>` `@apply` directives -- replace hardcoded gray classes within `@apply` with token equivalents: `bg-gray-100` -> `bg-surface-raised`, `hover:bg-gray-200` -> `hover:bg-border`, `text-gray-700` -> `text-text-primary`)

**Reports (2 files):**
- `web/reflekt-web/src/components/reports/ReportSection.vue` -- `bg-white`, `text-gray-400`, `text-gray-800`, `text-gray-700`, `text-gray-500`
- `web/reflekt-web/src/components/reports/SprintReportPanel.vue` -- `bg-white`, `text-gray-800`, `text-gray-500`, `text-gray-700`, `bg-blue-100 text-blue-800` (participant badges), `bg-gray-800` (copy button), `text-gray-400`, `bg-amber-50`, `text-amber-800`, `text-amber-600`, `bg-emerald-100`, `bg-amber-100`, `bg-gray-100`, `bg-red-50`

**Layout (1 file):**
- `web/reflekt-web/src/components/layout/PageLayout.vue` -- `text-gray-500`, `text-gray-400`

**Other (1 file):**
- `web/reflekt-web/src/components/PhaseIndicator.vue` -- `bg-gray-200` (inactive connector), `text-gray-400` (inactive label), `bg-white` (inactive circle), `border-gray-200` (inactive circle border). Also fix `ring-primary-100` on the active step circle -- on dark backgrounds this is a harsh bright ring. Replace with `ring-primary-500/20` which provides a subtle glow in both themes.

**Special cases:**
- Tooltip backgrounds (`bg-gray-800 text-white`) -- keep as-is in both themes. Dark-on-light and dark-on-dark tooltips both work because they use high contrast internally.
- Color swatch selection borders (`border-gray-800`) -- replace with `border-text-primary` so it contrasts in both themes.
- `PreviousActionsPanel` status button active states (`bg-emerald-600`, `bg-amber-500`, `bg-gray-500` with `text-white`) -- these are "filled" states that work in both themes. Keep the active state classes. Only the inactive state (`bg-white border-gray-300 text-gray-600 hover:bg-gray-50`) needs token migration.
- Drag-over highlight (`bg-primary-50/30`) -- replace with `bg-primary-500/10` which works on both light and dark surfaces.
- Focused group (`bg-primary-50`) -- replace with `bg-primary-500/10`.
- `RetroRoomView` closed-sprint banner (`bg-amber-50 border-amber-200 text-amber-800`) -- define theme-aware warning banner tokens or use `bg-warning/10 border-warning/30 text-warning`.
- Reporting phase tab toggle (`bg-gray-200 text-gray-700 hover:bg-gray-300`) -- replace with `bg-surface-raised text-text-secondary hover:bg-border`.

**Acceptance criteria:**
- All 22 feature component files use token-based color classes
- Tooltip backgrounds remain dark in both themes
- Color swatches display full saturated colors with visible selection indicator in both themes
- Status badges/buttons remain readable in both themes
- Drag-over and focus highlights are visible in both themes

### Step 7: Migrate views to theme tokens

**Skill:** None (mechanical class replacement)

**What:** Replace hardcoded color classes in all view files and add scrollbar styling.

**Files to modify (5 view files + 1 CSS file):**
- `web/reflekt-web/src/views/WelcomeView.vue` -- `text-gray-800`, `bg-blue-600` (use `bg-primary-600`), `hover:bg-blue-700` (use `hover:bg-primary-700`)
- `web/reflekt-web/src/views/TeamDashboardView.vue` -- `text-gray-800`, `bg-blue-600`, `hover:bg-blue-700`, `bg-gray-800` (tooltips -- keep), `text-gray-600`
- `web/reflekt-web/src/views/SprintView.vue` -- `bg-gray-50` (page background -- use `bg-background`), `text-gray-500`
- `web/reflekt-web/src/views/ReportView.vue` -- `text-gray-500`, `bg-white`, `text-red-600`
- `web/reflekt-web/src/views/RetroRoomView.vue` -- `text-gray-600`, `bg-amber-50`, `border-amber-200`, `text-amber-800`, `bg-gray-200`, `text-gray-700`, `hover:bg-gray-300`

**Verified clean (no changes needed):**
- `web/reflekt-web/src/views/SprintListView.vue` -- delegates entirely to SprintList component, no hardcoded color classes.

**Scrollbar styling** -- add to `web/reflekt-web/src/assets/main.css`:
- Dark mode scrollbar: narrow thumb (6px), thumb color = `var(--color-scrollbar-thumb)`, transparent track
- Light mode: browser default (no custom scrollbar rules under `.light`)
- Use `::-webkit-scrollbar`, `::-webkit-scrollbar-thumb`, `::-webkit-scrollbar-track` selectors scoped under `:root` or `.dark`

**Acceptance criteria:**
- All 5 view files use token-based color classes (SprintListView verified clean)
- Scrollbars in dark mode are subtle and blended
- Light mode uses browser-default scrollbars
- `bg-blue-600` buttons replaced with `bg-primary-600` for consistency

### Step 8: Build verification

**Skill:** None

**What:** Run the frontend build to verify no broken classes, missing tokens, or TypeScript errors.

**Commands:**
- `cd web/reflekt-web && npm run build` (runs `vue-tsc -b` + Vite)

**Acceptance criteria:**
- `npm run build` passes with zero errors
- No Tailwind warnings about unknown utility classes
- Both themes render correctly (manual visual check):
  - Dark: near-black background, dark cards with borders, readable text
  - Light: tinted white background, white cards with light borders, readable text
  - Theme toggle works instantly
  - No flash on page load
  - Column colors are vibrant in both themes
  - Badges are readable in both themes
  - Scrollbars are subtle in dark mode

## Cross-Service Changes

None. This feature is entirely frontend.

## Migration Notes

None. No database changes.

## Testing Strategy

- **Flash test:** Hard-reload in each theme, confirm no flash of the wrong theme.
- **Toggle test:** Click toggle repeatedly, verify instant switch with no flicker.
- **Persistence test:** Set light mode, close tab, reopen -- should render in light.
- **First visit test:** Clear localStorage, load app -- should render in dark.
- **Contrast spot-check:** Verify body text against background meets 4.5:1 ratio in both themes (use browser DevTools contrast checker on a few representative elements).
- **Column colors test:** Verify column headers are full saturated color, column backgrounds are tinted (dark) or solid light (light) in both themes.
- **Badge readability test:** Check all badge variants (Draft, Active, Closed, success, danger, warning, info) are readable in both themes.
- **Form elements test:** Verify inputs, selects, textareas have visible borders and readable text in both themes.
- **Scrollbar test:** Verify dark mode scrollbars are narrow and blended, light mode uses browser defaults.
- **Focus ring test:** Tab through interactive elements in dark mode, verify no white gap around focus rings.

## KB Impact

None. This is a purely visual/frontend feature with no business rule changes.

## Open Questions

None. All decisions resolved:
- Q1 (shadows): Card shadows suppressed in dark, modal shadows kept. Confirmed.
- Q2 (`:root` default): `:root` = dark, `.light` override. Confirmed.

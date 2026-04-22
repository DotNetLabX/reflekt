# Foundation UI Stylization (Plan 1 of 3)

## Context

The Reflekt frontend is functionally complete but visually unstyled beyond raw Tailwind defaults. Every view re-implements buttons, inputs, and badges with inconsistent padding, focus rings, and heading sizes. There are no design tokens, no shared components, no icon library, and 10 unused scaffold files from `create-vue`. The document title still says "Vite App".

This plan establishes the visual foundation that Plan 2 (RetroRoom) and Plan 3 (Remaining Views + Polish) build on. Nothing in this plan changes application behavior — it's pure styling infrastructure.

**Prior research:** `ai-coding/plans/Stylization/research.md` contains competitive analysis (6 retro tools) and a full per-view audit with prioritized punch list. This plan implements items 1.1–1.5 and 6.2 from that punch list.

## Scope

**In scope:**
- Design tokens (color palette, typography, spacing, shadows, border-radius) as Tailwind v4 `@theme` extensions
- Shared component library: BaseButton, BaseInput, BaseBadge, BaseCard, BaseModal, LoadingSpinner, EmptyState
- AppHeader redesign with app name, navigation, and user menu
- Lucide Vue icon library integration, replacing inline SVGs and emoji icons
- Scaffold cleanup (remove unused files, dead CSS, counter store)
- Page title fix (index.html + per-route document titles)

**Explicitly out of scope:**
- Restyling RetroRoomView, SprintView, ReportView, WelcomeView (Plan 2 and Plan 3)
- Responsive breakpoints for the retro board (Plan 2)
- Animations and transitions (Plan 2)
- Toast notification system (Plan 2)
- Phase stepper component (Plan 2 — RetroRoom + SprintView)
- Dark mode
- Accessibility audit (Plan 3)

## Domain Model Changes

None. This plan is frontend-only.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Scaffold cleanup

Delete unused files left over from `create-vue`. Remove the `base.css` import that may conflict with Tailwind's reset.

**Delete these files:**
- `web/reflekt-web/src/components/HelloWorld.vue`
- `web/reflekt-web/src/components/TheWelcome.vue`
- `web/reflekt-web/src/components/WelcomeItem.vue`
- `web/reflekt-web/src/components/icons/IconCommunity.vue`
- `web/reflekt-web/src/components/icons/IconDocumentation.vue`
- `web/reflekt-web/src/components/icons/IconEcosystem.vue`
- `web/reflekt-web/src/components/icons/IconSupport.vue`
- `web/reflekt-web/src/components/icons/IconTooling.vue`
- `web/reflekt-web/src/views/HomeView.vue`
- `web/reflekt-web/src/views/AboutView.vue`
- `web/reflekt-web/src/stores/counter.ts`
- `web/reflekt-web/src/assets/base.css`
- `web/reflekt-web/src/assets/logo.svg`

**Modify:**
- `web/reflekt-web/src/assets/main.css` — remove the `@import "./base.css"` line (currently only contains `@import "tailwindcss";` so just verify `base.css` isn't imported elsewhere)

**Verify:** `npm run type-check` passes after deletion. Grep the `src/` directory for any remaining import references to deleted files.

### Step 2: Design tokens via Tailwind v4 `@theme`

Define the project's visual identity as CSS custom properties using Tailwind v4's `@theme` directive in `main.css`. This is how Tailwind v4 handles theme customization (no `tailwind.config.js`).

**Modify:** `web/reflekt-web/src/assets/main.css`

Add a `@theme` block before `@import "tailwindcss"` with these token categories:

**Color palette** — clean, modern, Linear/Notion-inspired:
- **Brand primary:** An indigo/violet range (e.g. `--color-primary-50` through `--color-primary-900`). Use as the main accent for buttons, links, active states.
- **Semantic column colors:** Soft tints for retro columns — green (went well), red/rose (didn't go well), blue (action items). Keep existing green/red/blue families but define specific shades that feel cohesive.
- **Neutrals:** A cool gray scale (`--color-gray-50` through `--color-gray-950`) for text, backgrounds, borders. Slightly cooler than Tailwind defaults to match the modern aesthetic.
- **Surface colors:** `--color-surface` (white/near-white card backgrounds), `--color-surface-raised` (slightly elevated panels), `--color-background` (page background — a warm off-white, not pure white).
- **Semantic:** `--color-success`, `--color-danger`, `--color-warning`, `--color-info`.

**Typography:**
- Font family: Inter (via Google Fonts import or system font stack with Inter first). Add the font import to `index.html` or `main.css`.
- Font sizes: Use Tailwind defaults but define `--font-size-page-title`, `--font-size-section-title`, `--font-size-body`, `--font-size-caption` if needed for consistency reference.

**Spacing & Sizing:**
- Use Tailwind's default spacing scale (it's already good). No custom overrides needed.

**Shadows:**
- `--shadow-card`: subtle multi-layer shadow for cards (reference Parabol's approach: `0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06)`)
- `--shadow-card-hover`: slightly elevated on hover
- `--shadow-modal`: deeper shadow for modals/overlays

**Border radius:**
- `--radius-sm`: 4px (badges, small chips)
- `--radius-md`: 8px (cards, inputs, buttons)
- `--radius-lg`: 12px (modals, large panels)
- `--radius-full`: 9999px (avatar circles, pills)

**Reference:** Tailwind v4 `@theme` docs. The executor should verify the exact syntax since Tailwind v4's CSS-first config is relatively new.

### Step 3: Install Lucide Vue icons

Install the `lucide-vue-next` package and set it up for tree-shaking.

**Run:** `npm install lucide-vue-next` in `web/reflekt-web/`

**Delete:** The entire `web/reflekt-web/src/components/icons/` directory (files already deleted in Step 1, now remove the empty directory).

No global registration needed — Lucide icons are imported individually per component. The executor should verify the correct package name and import syntax for the current version.

### Step 4: Shared component library — BaseButton

Create `web/reflekt-web/src/components/ui/BaseButton.vue`

A button component that consolidates all the inconsistent button styles found in the audit (6+ different padding/color combinations).

**Props:**
- `variant`: `'primary' | 'secondary' | 'danger' | 'ghost'` (default: `'primary'`)
- `size`: `'sm' | 'md' | 'lg'` (default: `'md'`)
- `loading`: `boolean` (default: `false`) — shows a spinner and disables
- `disabled`: `boolean`

**Styling per variant (using design tokens from Step 2):**
- `primary`: brand primary background, white text, hover darkens
- `secondary`: gray/neutral border, neutral text, hover fills lightly
- `danger`: red/rose background, white text
- `ghost`: transparent, text-colored, hover shows light fill

**All variants must have:**
- `focus-visible:ring-2 focus-visible:ring-offset-2` with brand primary ring color
- Disabled state: `opacity-50 cursor-not-allowed`
- Loading state: icon spins, button text preserved, pointer-events-none

**Slots:** Default slot for button content. Optional `icon` slot (left-aligned before text).

**Pattern reference:** This is a new file. Follow Vue 3 `<script setup lang="ts">` with `defineProps` and computed class binding.

### Step 5: Shared component library — BaseInput

Create `web/reflekt-web/src/components/ui/BaseInput.vue`

Consolidates the 3+ different input padding and focus ring combinations.

**Props:**
- `modelValue`: `string | number`
- `type`: `string` (default: `'text'`)
- `placeholder`: `string`
- `maxlength`: `number`
- `disabled`: `boolean`
- `error`: `string` — when provided, border turns red and error text shows below

**Emits:** `update:modelValue` (for v-model support)

**Styling:**
- Consistent padding: `px-3 py-2`
- Border: `border border-gray-300` (or neutral token), `rounded-md` (using `--radius-md`)
- Focus: `focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500`
- Error state: red border, red ring, error message below in `text-sm text-danger`
- Disabled: `bg-gray-50 text-gray-400 cursor-not-allowed`

### Step 6: Shared component library — BaseBadge

Create `web/reflekt-web/src/components/ui/BaseBadge.vue`

Used for sprint state badges (Draft, Active, Closed), phase labels, vote counts.

**Props:**
- `variant`: `'default' | 'success' | 'danger' | 'warning' | 'info' | 'primary'` (default: `'default'`)
- `size`: `'sm' | 'md'` (default: `'sm'`)

**Styling:**
- Pill-shaped: `rounded-full` (using `--radius-full`)
- Small: `px-2 py-0.5 text-xs font-medium`
- Each variant maps to a background/text color pair from the design tokens

### Step 7: Shared component library — BaseCard

Create `web/reflekt-web/src/components/ui/BaseCard.vue`

A surface container used everywhere (sprint list items, retro cards, report sections, form containers).

**Props:**
- `padding`: `'none' | 'sm' | 'md' | 'lg'` (default: `'md'`)
- `hoverable`: `boolean` (default: `false`) — adds hover shadow elevation

**Styling:**
- Background: `--color-surface`
- Border: `border border-gray-200`
- Shadow: `--shadow-card`
- Radius: `--radius-md`
- Hoverable: transitions to `--shadow-card-hover` on hover

**Slots:** Default slot for content. Optional `header` and `footer` named slots with border-bottom/top dividers.

### Step 8: Shared component library — BaseModal

Create `web/reflekt-web/src/components/ui/BaseModal.vue`

Needed for delete confirmations (replacing browser `confirm()`) and destructive action confirmations. Plan 2 will use this immediately.

**Props:**
- `open`: `boolean`
- `title`: `string`

**Emits:** `close`

**Structure:**
- Teleport to `<body>`
- Backdrop: fixed overlay with `bg-black/50` and click-to-close
- Panel: centered, `max-w-md`, `--shadow-modal`, `--radius-lg`
- Header: title + close (X) button using Lucide `X` icon
- Body: default slot
- Footer: named `footer` slot (for action buttons)
- Escape key closes

**Transitions:** Vue `<Transition>` — fade backdrop, scale+fade panel.

### Step 9: Shared component library — LoadingSpinner

Create `web/reflekt-web/src/components/ui/LoadingSpinner.vue`

Replaces all "Loading..." plain text throughout the app.

**Props:**
- `size`: `'sm' | 'md' | 'lg'` (default: `'md'`)

**Styling:**
- SVG spinner with `animate-spin` (Tailwind's built-in)
- Uses brand primary color
- Sizes: sm=16px, md=24px, lg=40px
- Optionally wrapped in a centered container for full-page loading: consider an optional `label` prop or default slot for "Loading..." text below the spinner

### Step 10: Shared component library — EmptyState

Create `web/reflekt-web/src/components/ui/EmptyState.vue`

Replaces all the identical `text-gray-400 text-sm` empty state messages.

**Props:**
- `title`: `string` (e.g. "No cards yet")
- `description`: `string` (optional, secondary text)
- `icon`: component (optional — a Lucide icon to display above the title)

**Slots:** Default slot for optional action (e.g. a button "Create your first sprint")

**Styling:**
- Centered flex column
- Icon: `w-12 h-12 text-gray-300` using the passed Lucide component
- Title: `text-sm font-medium text-gray-500`
- Description: `text-xs text-gray-400`
- Generous vertical padding: `py-8`

### Step 11: AppHeader redesign

Modify `web/reflekt-web/src/components/AppHeader.vue`

Transform from a debug-bar (email + sign-out link) into a proper app header.

**Layout:**
- Left: App name "Reflekt" in `font-semibold text-lg` using brand primary color. Use a Lucide icon (e.g. `MessageSquare`, `Repeat`, or `RefreshCw`) as a small logo mark beside the name. Link to `/` (welcome/dashboard).
- Right (signed in): User menu — avatar circle showing first initial of `displayName` or `givenName` with brand-colored background. On click: dropdown with email (non-interactive, display only) and "Sign out" button.
- Right (signed out): Nothing, or a subtle "Sign in" link.

**Styling:**
- Sticky top (not fixed — no overlap issues)
- White background with bottom border: `border-b border-gray-200`
- Height: consistent ~56px (`h-14`)
- Horizontal padding: `px-4 sm:px-6`
- `z-10` to stay above content

**User dropdown:**
- Toggle on click (not hover)
- Click-outside-to-close
- Use `BaseCard` with `shadow-lg` or similar elevated treatment
- Items: email (muted text, non-clickable), divider, Sign out button

**Modify:** `web/reflekt-web/src/App.vue` — adjust the layout if needed. The `<RouterView>` should sit below the sticky header without overlap. Ensure the `min-h-screen` centering in views accounts for the header height.

### Step 12: Page title fix

**Modify:** `web/reflekt-web/index.html`
- Change `<title>Vite App</title>` to `<title>Reflekt</title>`

**Modify:** `web/reflekt-web/src/router/index.ts`
- Add `meta: { title: string }` to each route definition:
  - `/` → "Reflekt"
  - `/not-invited` → "Access Denied — Reflekt"
  - `/teams/:teamId/sprints/:sprintId` → "Sprint — Reflekt"
  - `/sprint/:sprintId` → "Retro Room — Reflekt"
  - `/teams/:teamId/sprints/:sprintId/report` → "Report — Reflekt"
- Add a `router.afterEach` guard that sets `document.title = to.meta.title || 'Reflekt'`

### Step 13: Barrel export for UI components

Create `web/reflekt-web/src/components/ui/index.ts`

Re-export all shared components for cleaner imports:
```
export { default as BaseButton } from './BaseButton.vue'
export { default as BaseInput } from './BaseInput.vue'
// etc.
```

This lets consumers write `import { BaseButton, BaseInput } from '@/components/ui'`.

### Step 14: Verify build

Run `npm run build` in `web/reflekt-web/` to ensure:
- No import errors from deleted scaffold files
- No TypeScript errors from new components
- Vite build succeeds and outputs to `wwwroot/`

Also run `npm run type-check` separately to catch any type issues.

## Cross-Service Changes

None. Frontend-only plan.

## Migration Notes

None. No data model changes.

## Testing Strategy

- **Build verification:** `npm run build` and `npm run type-check` must pass.
- **Visual verification:** Start the dev server (`npm run dev`) and check:
  - AppHeader shows correctly on all routes
  - Page titles update per route
  - No broken imports or missing components
  - Design tokens apply (check that `@theme` values are reflected in computed styles)
- **Component spot-check:** Temporarily render each shared component (BaseButton with all variants, BaseInput with error state, BaseBadge variants, BaseModal open, LoadingSpinner, EmptyState) in a scratch view or Storybook-lite approach to confirm they render correctly. Remove the scratch view after verification.
- **Regression:** Existing views should still render and function (they still use inline Tailwind classes — the shared components are not yet wired into views in this plan).

## Open Questions

None. All decisions are made based on the research in `ai-coding/plans/Stylization/research.md` and the constraints in CLAUDE.md (Tailwind CSS, no full UI kits, shadcn-vue acceptable if lightweight).

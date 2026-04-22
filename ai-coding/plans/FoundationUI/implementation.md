# Foundation UI Stylization — Implementation

## Files Deleted
- `web/reflekt-web/src/components/HelloWorld.vue` — unused create-vue scaffold
- `web/reflekt-web/src/components/TheWelcome.vue` — unused create-vue scaffold
- `web/reflekt-web/src/components/WelcomeItem.vue` — unused create-vue scaffold
- `web/reflekt-web/src/components/icons/IconCommunity.vue` — unused scaffold icon
- `web/reflekt-web/src/components/icons/IconDocumentation.vue` — unused scaffold icon
- `web/reflekt-web/src/components/icons/IconEcosystem.vue` — unused scaffold icon
- `web/reflekt-web/src/components/icons/IconSupport.vue` — unused scaffold icon
- `web/reflekt-web/src/components/icons/IconTooling.vue` — unused scaffold icon
- `web/reflekt-web/src/views/HomeView.vue` — unused scaffold view
- `web/reflekt-web/src/views/AboutView.vue` — unused scaffold view
- `web/reflekt-web/src/stores/counter.ts` — unused scaffold store
- `web/reflekt-web/src/assets/base.css` — old Vue template CSS (conflicting resets, dark mode vars)
- `web/reflekt-web/src/assets/logo.svg` — unused Vue logo
- `web/reflekt-web/src/components/icons/` — empty directory removed

## Files Created
- `web/reflekt-web/src/components/ui/BaseButton.vue` — button component with primary/secondary/danger/ghost variants, sm/md/lg sizes, loading state with spinner
- `web/reflekt-web/src/components/ui/BaseInput.vue` — input component with v-model support, error state, disabled state
- `web/reflekt-web/src/components/ui/BaseBadge.vue` — badge/pill component with default/success/danger/warning/info/primary variants
- `web/reflekt-web/src/components/ui/BaseCard.vue` — surface container with header/footer slots, padding options, hoverable shadow elevation
- `web/reflekt-web/src/components/ui/BaseModal.vue` — modal dialog with teleport to body, backdrop, escape-to-close, enter/leave transitions, scroll lock
- `web/reflekt-web/src/components/ui/LoadingSpinner.vue` — spinner with sm/md/lg sizes, optional label slot
- `web/reflekt-web/src/components/ui/EmptyState.vue` — empty state placeholder with icon, title, description, action slot
- `web/reflekt-web/src/components/ui/index.ts` — barrel re-export for all ui components

## Files Modified
- `web/reflekt-web/src/assets/main.css` — replaced single tailwind import with full `@theme` block: brand primary (indigo), retro column colors, surface colors, semantic status colors, shadow tokens, border-radius tokens, Inter font family on body, background color
- `web/reflekt-web/index.html` — added Google Fonts preconnect + Inter font stylesheet link, changed title from "Vite App" to "Reflekt"
- `web/reflekt-web/src/components/AppHeader.vue` — full redesign: sticky header with Reflekt branding (RefreshCw icon + name), avatar initial circle for signed-in users, dropdown menu with email display and sign out, click-outside-to-close, enter/leave transition
- `web/reflekt-web/src/router/index.ts` — added `RouteMeta` type augmentation with `title` field, added `meta.title` to each route, added `router.afterEach` guard for `document.title`
- `web/reflekt-web/package.json` — lucide-vue-next added as dependency (via npm install)

## Key Decisions
- Used `LoaderCircle` from lucide-vue-next for both BaseButton loading state and LoadingSpinner (consistent spinner icon across the app)
- Used `RefreshCw` icon as Reflekt logo mark — matches the "reflect/retrospective" concept
- Google Fonts loaded via `<link>` in index.html (preconnect + stylesheet) rather than CSS `@import` for better loading performance
- BaseModal uses scoped CSS for Vue Transition animations since Tailwind can't target transition states
- AppHeader dropdown uses template ref + `contains()` for click-outside detection
- Tailwind v4 `@theme` syntax: tokens defined as CSS custom properties, referenced in classes via `rounded-[--radius-md]` etc.

## Deviations from Plan
- None. All 14 steps implemented as specified.

## Review Fixes (Cycle 1)
- **[HIGH] BaseModal scroll lock leak** — added `onUnmounted` cleanup to restore `document.body.style.overflow` when component is destroyed while open
- **[MEDIUM] AppHeader click-outside** — replaced fragile `.closest('.relative')` with template ref (`menuRef`) + `contains()` check
- **[LOW] HTML lang attribute** — changed `<html lang="">` to `<html lang="en">` in index.html

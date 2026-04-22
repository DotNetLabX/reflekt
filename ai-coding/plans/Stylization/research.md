# Stylization — Research

**Date:** 2026-04-21
**Source:** Competitive research (6 retro tools) + full frontend audit (all Vue views and components)

---

## Part 1: Competitive Research

### Tool-by-Tool Findings

#### EasyRetro (easyretro.io) — Most Popular
- **Layout:** Column-based Kanban board. Background image upload for theming.
- **Cards:** Colored sticky-note style, per-card color changes. Drag-and-drop, grouping/merging. Inline images/GIFs.
- **Voting:** Configurable dot voting with counter on each card. Cards sortable by vote count. Facilitator can hide vote counts.
- **Phases:** No rigid flow — facilitator controls manually. "Hide Cards" mode blurs cards during input.
- **Dark mode:** Yes, togglable.
- **Standout:** Auto-scroll while dragging. Highlight Mode (hover highlights across all screens). QR code invite. 100+ templates.

#### Parabol (parabol.co) — Best UX, Open Source
- **Layout:** Phase-driven meeting flow with prominent "Next" button. Built-in timer per phase. Phases: Icebreaker -> Reflect -> Group -> Vote -> Discuss.
- **Palette (from source `paletteV3.ts`):** 13 color families x 9 shades. Brand: Grape `#493272` (deep purple). Slate 100-200 for backgrounds, 700-900 for text. Key accents: Tomato `#FD6157`, Sky `#329AE5`, Jade `#40B574`.
- **Cards:** `border-radius: 4px`. Multi-layer box shadow: `rgba(0,0,0,.2) 0px 2px 1px -1px, rgba(0,0,0,.14) 0px 1px 1px 0px, rgba(0,0,0,.12) 0px 1px 3px 0px`. 16px row gutter. Color-coded prompt label per column.
- **Voting:** Interactive anonymous voting with emoji reactions.
- **Standout:** AI auto-naming of grouped clusters. Async chat within meeting. Task integration to Jira/GitHub.

#### Metro Retro / Ludi (metroretro.io) — Playful
- **Layout:** Infinite whiteboard canvas (not column-locked). Zones as containers. Flexbox-inspired layout engine.
- **Cards:** Two sticky sizes (regular/large). Per-user handwriting-style font on stickies.
- **Visual tools:** Toolbar with stickies, index cards, marker pen, tokens, connectors, voting tool.
- **Style:** Game-like aesthetic. Confetti, gestures, hats.
- **Standout:** Double-click canvas to create sticky. Vanishing pen for temp annotations.

#### TeamRetro (teamretro.com) — Enterprise
- **Layout:** Strict guided phases: Set Stage -> Brainstorm/Vote -> Group Ideas -> Facilitate Discussion -> Create Actions -> Summary -> Insights.
- **Cards:** Sticky-note style with dot voting. AI auto-suggest for grouping.
- **Voting:** Dot voting on cards. Time-boxed, private.
- **Standout:** Presentation mode (one card at a time for discussion). Dedicated Insights dashboard with sentiment tracking and trends. SOC 2 Type 2.

#### Retrium (retrium.com) — Data-Driven
- **Layout:** Five explicit phases: Think -> Group -> Vote -> Discuss -> Wrap Up. Facilitator gates advancement.
- **Cards:** Anonymous during Think. Text blurred while typing, revealed simultaneously.
- **Voting:** ~5 dot votes per participant. Highest-voted surface first in Discussion.
- **Phase indicators:** Named phase screens with timer. "Return of Time Invested" metric in Wrap Up.
- **Style:** Functional aesthetic (some reviewers note it "looks old" but praise the flow).

#### FunRetro — Predecessor to EasyRetro
Rebranded to EasyRetro in 2022. Same patterns apply.

### Common Patterns Across Best Retro Tools

| Pattern | Industry Standard |
|---------|-------------------|
| **Board layout** | Column-based (3-4 columns mapped to retro prompts) |
| **Cards** | Sticky-note style. Rounded corners (4px+). Subtle multi-layer shadows. Color-coded per column. Drag-and-drop. |
| **Voting** | Dot voting. Numeric counter/badge on card. Facilitator-configurable budget (3-5 votes). Cards auto-sort by vote count. |
| **Phase progression** | Guided flow with explicit advancement. Named phases with stepper/indicator. Visible timer. |
| **Privacy** | Cards hidden/blurred during input. Revealed simultaneously or per-column. Anonymous by default. |
| **Colors** | Purple/grape as brand accent (Parabol, Retrium). Warm pastels for card backgrounds. High-contrast neutral scale for chrome. 12+ colors for column differentiation. |
| **Management vs. board** | Clear separation: retro board (real-time) vs. dashboard/insights (analytics, trends). |
| **Typography** | Sans-serif universally. Clear hierarchy. |
| **Whitespace** | 16px gutters between cards. Moderate density. |
| **Animations** | Drag-and-drop, card highlight across participants, phase transitions with distinct screen changes. |

---

## Part 2: Current State Audit

### Architecture Overview

```
App.vue
  AppHeader.vue (email + sign-out, 17 lines)
  RouterView
    WelcomeView.vue (landing/dashboard, 166 lines)
    SprintView.vue (admin, 200 lines)
    RetroRoomView.vue (retro board, 485 lines)
      CardItem.vue (only reusable component, 237 lines)
    ReportView.vue (post-retro report, 188 lines)
    NotInvitedView.vue (access denied, 8 lines)
```

No component library. No shared button/input/badge/modal components. Every view re-implements styling via raw Tailwind classes.

### Tailwind Configuration
- **Tailwind v4.2.2** via Vite plugin. **Zero theme customization.** No custom colors, fonts, or design tokens.
- `base.css` from Vue scaffold is loaded but unused — its `* { font-weight: normal }` may conflict with Tailwind's reset.
- `index.html` title still says "Vite App".

### Per-View Findings

#### RetroRoomView.vue (core experience — where users spend most time)
- **Layout:** `grid-cols-3` / `grid-cols-2`. Columns have `max-h-[60vh]` with overflow scroll. **Not responsive** — no breakpoints at all. Breaks on mobile/tablet.
- **Colors:** Semantic column colors (green/red/blue) using default Tailwind values. Functional but generic — no brand identity.
- **Missing UX:**
  - No loading state between join and room render
  - No phase indicator visible to participants (phase drives UI but is never shown)
  - No card add/remove animations (instant pop in/out)
  - Toast is error-only (red), no success toasts
  - Vote button uses text triangle (`▲`) instead of an icon
  - No confirmation dialog for card delete
  - No character count on textarea

#### SprintView.vue (admin page)
- **Layout:** Centered single card (`max-w-lg`). Well-structured.
- **Missing UX:**
  - Loading state is plain text "Loading sprint..."
  - No phase stepper/progress bar (Writing -> Voting -> Discussing -> Reporting)
  - Kick confirmation uses browser `confirm()` dialog
  - No confirmation for Close Sprint (destructive)

#### ReportView.vue (post-retro)
- **Layout:** Stacked cards at `max-w-4xl`. Column colors match retro room.
- **Missing UX:**
  - Loading state is plain text
  - Card items are plain `<li>` elements — no card treatment, just text
  - No vote counts shown despite voting having happened
  - No print/export styling

#### WelcomeView.vue (first impression)
- **Layout:** Three states with inconsistent sizing (`max-w-sm`, `max-w-md`, `w-72`).
- **Missing UX:**
  - Not-signed-in state has no background card — just floating text on bare page
  - "Sign in with Google" is a plain blue button — no Google icon/branding
  - No loading state while fetching teams/sprints
  - Inconsistent heading sizes across the three states

#### NotInvitedView.vue (access denied)
- Just a single `<p>` tag. No icon, no illustration, no action (no sign-out, no "contact admin" link).

#### AppHeader.vue (header bar)
- Email + "Sign out" link. No app logo, no brand name, no navigation.

#### CardItem.vue (retro cards)
- `bg-white border rounded p-3 shadow-sm`. Clean but basic.
- Uses emoji characters for icons (chain link, multiplication sign) — inconsistent cross-platform.
- No transition between view/edit mode.
- Evidence link text truncated at arbitrary `max-w-[180px]`.

### Cross-Cutting Issues

#### Consistency Problems
| Element | Variants Found |
|---------|----------------|
| Input padding | `px-3 py-2`, `px-2 py-1`, `px-2 py-0.5` |
| Focus ring color | `blue-500`, `green-400`, `red-400`, `blue-400` |
| Button padding | `px-4 py-2`, `px-3 py-1`, `px-2 py-1`, `px-2 py-0.5` |
| Heading styles | `2xl semibold`, `2xl bold`, `xl bold`, `lg semibold` |
| Border styles | `border` (default), `border border-gray-300` |
| Shadows | `shadow`, `shadow-sm` |

#### Responsiveness
**Effectively zero.** No `sm:`, `md:`, `lg:`, `xl:` breakpoints in any real view. The retro board's 3-column grid is completely broken on mobile. Participants likely join from phones via invite link.

#### Animations & Transitions
Two instances in the entire app:
1. `animate-pulse` on pending evidence link
2. `transition-colors` on vote button

No page transitions, no card animations, no modal transitions, no loading spinners.

#### Loading States
Every view: either no loading state or plain text ("Loading..."). No spinners, no skeletons.

#### Empty States
All identical: `text-gray-400 text-sm` with minimal text. No illustrations, no icons, no guidance.

#### Scaffold Cruft (10 unused files)
- `HomeView.vue`, `AboutView.vue` (placeholder routes)
- `HelloWorld.vue`, `TheWelcome.vue`, `WelcomeItem.vue` (unused components)
- 5x `Icon*.vue` files (scaffold icons)
- `base.css` (loaded, unused, potentially conflicting)

---

## Part 3: Punch List

### 1. Foundation (affects everything)

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 1.1 | **Design tokens / theme** | Zero Tailwind customization. All colors are default values. No brand identity. | Define custom palette in Tailwind v4 `@theme`: brand primary (consider purple/grape like Parabol or a unique accent), semantic column colors, neutral scale, surface colors. Custom font (Inter or similar modern sans-serif). | MUST |
| 1.2 | **Shared component library** | No reusable components. Every view re-implements buttons, inputs, badges. Root cause of all consistency issues. | Extract: `BaseButton` (variants: primary, secondary, danger, ghost; sizes: sm, md, lg), `BaseInput`, `BaseBadge`, `BaseCard`, `BaseModal`, `EmptyState`, `LoadingSpinner`. | MUST |
| 1.3 | **AppHeader** | No logo, no brand name, no navigation. Feels like a debug bar. | App name/logo left-aligned, navigation (Dashboard, current retro if active), user menu right-aligned with avatar initial circle + dropdown (email, sign out). | MUST |
| 1.4 | **Page title** | `index.html` says "Vite App" | "Reflekt" (dynamic per-route titles optional) | MUST |
| 1.5 | **Scaffold cleanup** | 10 unused files from `create-vue`. `base.css` may conflict with Tailwind. | Delete all scaffold files. Remove `base.css` import. | MUST |

### 2. RetroRoomView (highest impact)

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 2.1 | **Phase indicator** | No visible phase. Users can't tell if they're in Writing, Voting, or Discussing. | Horizontal stepper at top of board showing all phases with current highlighted. Like Parabol/TeamRetro's guided flow. | MUST |
| 2.2 | **Card design** | Basic `border rounded shadow-sm`. Feels flat. | Sticky-note aesthetic: slightly larger border-radius (8px), warmer shadow, subtle color tint per column (pale green/red/blue background on the card itself, not just the column header). Reference: EasyRetro/Parabol card style. | MUST |
| 2.3 | **Column headers** | Plain colored bars with text. | More visual weight: icon per column type, card count badge, better typography hierarchy. | SHOULD |
| 2.4 | **Card animations** | Cards pop in/out instantly. | Vue `<TransitionGroup>` for card add/remove (fade + slide). Smooth card reordering. | MUST |
| 2.5 | **Vote button** | Text triangle (`▲`). | Proper icon (thumbs up, heart, or upvote arrow via Heroicons or Lucide). Animated count change. | SHOULD |
| 2.6 | **Loading state** | None between join and room render. | Full-page spinner or skeleton board while loading. | MUST |
| 2.7 | **Join form** | Functional but plain centered card. | More welcoming: app branding, sprint name displayed, participant avatars of who's already in (initial circles). | SHOULD |
| 2.8 | **Responsive layout** | Zero breakpoints. 3-column grid breaks on mobile. | Tab-based column switching on mobile, 2-col on tablet, 3-col on desktop. Participants join from phones. | MUST |
| 2.9 | **Toast notifications** | Error-only, bottom-right red box. | Styled toast system with success/error/info variants. Position top-center or top-right. Auto-dismiss with progress bar. | SHOULD |
| 2.10 | **Card delete confirmation** | No confirmation despite function named `confirmDelete`. | Styled modal: "Delete this card? This can't be undone." | SHOULD |

### 3. SprintView (admin)

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 3.1 | **Phase progression** | No visual indicator of sprint lifecycle. | Phase stepper showing Draft -> Writing -> Voting -> Discussing -> Closed with current phase highlighted. | MUST |
| 3.2 | **Loading state** | Plain text "Loading sprint..." | Spinner or skeleton. | SHOULD |
| 3.3 | **Destructive confirmations** | `confirm()` for kick. No confirm for Close Sprint. | Styled modal for all destructive actions (kick, close). | SHOULD |
| 3.4 | **Participant list** | Basic text list with kick button. | Avatar circles (initial letter), display name, join time, kick icon with hover state. | SHOULD |

### 4. ReportView

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 4.1 | **Card treatment** | Plain `<li>` text. No visual relationship to the retro cards. | Card-style rendering matching the retro room. Show vote count per item. | MUST |
| 4.2 | **Vote counts** | Not shown despite being the filter criteria. | Display vote count badge on each item (users want to know what got the most votes). | SHOULD |
| 4.3 | **Print styling** | None. Report is meant to be shared. | `@media print` styles for clean printout. | NICE |

### 5. WelcomeView / Login (first impression)

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 5.1 | **Sign-in page** | Plain text on bare page with a blue button. No branding. | Branded landing: app name/tagline, illustration or subtle pattern background, proper Google sign-in button with Google icon (per Google branding guidelines). | MUST |
| 5.2 | **Dashboard** | Functional but plain list. | Sprint cards instead of list rows. Visual status (Draft/Active/Closed with color and icon). Quick-action buttons. | SHOULD |
| 5.3 | **Empty team state** | "Create your first team" with a plain input. | More welcoming onboarding feel — illustration, guidance text. | NICE |

### 6. Cross-Cutting

| # | Item | What's Wrong | What It Should Be | Priority |
|---|------|-------------|-------------------|----------|
| 6.1 | **NotInvitedView** | Single `<p>` tag. Dead end. | Icon/illustration, explanation, sign-out button, "contact your admin" guidance. | SHOULD |
| 6.2 | **Icons** | Emoji characters (`▲`, chain link, `×`). Inconsistent cross-platform. | Icon library: Lucide Vue or Heroicons. Consistent size/weight. | MUST |
| 6.3 | **Page transitions** | None. Hard cuts between routes. | Vue `<Transition>` on `<RouterView>` for subtle fade between pages. | NICE |
| 6.4 | **Button focus states** | No focus styling on buttons. | `focus-visible:ring-2` on all interactive elements. | SHOULD |
| 6.5 | **Accessibility** | No ARIA attributes on real components. No keyboard nav for card actions. | ARIA labels on buttons, live regions for toasts, keyboard-accessible card actions. | SHOULD |

---

## Priority Summary

**MUST (13 items):** Foundation theme, component library, AppHeader, page title, scaffold cleanup, phase indicator, card design, card animations, loading states, responsive retro board, report card treatment, sign-in branding, icon library.

**SHOULD (13 items):** Column headers, vote button, join form, toast system, delete confirmation, sprint phase stepper, sprint loading, destructive confirmations, participant list, report vote counts, dashboard cards, NotInvitedView, button focus states, accessibility.

**NICE (3 items):** Print styling, empty team onboarding, page transitions.

### Recommended Implementation Order
1. Foundation (1.1-1.5) — everything depends on this
2. RetroRoomView MUST items (2.1, 2.2, 2.4, 2.6, 2.8) — highest user impact
3. WelcomeView sign-in (5.1) — first impression
4. Icons (6.2) — needed before other view work
5. SprintView phase stepper (3.1)
6. ReportView card treatment (4.1)
7. SHOULD items by view
8. NICE items

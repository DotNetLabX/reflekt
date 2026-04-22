# RetroRoom Stylization

## Context

The RetroRoom is where participants spend the vast majority of their time in Reflekt. Foundation work (design tokens, shared component library, AppHeader, page title, scaffold cleanup) is complete. The shared components (`BaseButton`, `BaseInput`, `BaseBadge`, `BaseCard`, `BaseModal`, `LoadingSpinner`, `EmptyState`) exist in `web/reflekt-web/src/components/ui/` but are not yet adopted in RetroRoomView or CardItem.

This plan covers research punch list items 2.1-2.10: the visual and UX overhaul of the retro board experience.

## Scope

**In scope (items 2.1-2.10):**
- Phase indicator stepper
- Card redesign (sticky-note aesthetic, shared component adoption)
- Column header enhancement (icons, card count badge, typography)
- Card add/remove animations (TransitionGroup)
- Vote button redesign (Lucide icon, animated count)
- Loading state (spinner while entering room)
- Join form branding (sprint name, participant avatars, shared components)
- Responsive layout (mobile tabs, tablet 2-col, desktop 3-col)
- Toast notification system (success/error/info with auto-dismiss)
- Card delete confirmation modal

**Explicitly out of scope:**
- SprintView (admin page) stylization — separate plan
- ReportView stylization — separate plan
- WelcomeView / sign-in page — separate plan
- Dark mode
- Drag-and-drop card reordering
- Page transitions on RouterView
- Print/export styling
- Backend changes — this is frontend-only

## Domain Model Changes

None. No backend changes needed.

## Data Model Changes

None.

## Implementation Steps

### Step 1: Toast Notification System

Create a composable + component for toast notifications. This is a dependency for later steps — the current inline `showToast` in RetroRoomView will be replaced.

**Files to create:**
- `web/reflekt-web/src/composables/useToast.ts` — reactive toast state composable (add/remove toasts, auto-dismiss timer with configurable duration)
- `web/reflekt-web/src/components/ui/ToastContainer.vue` — positioned container that renders active toasts with enter/leave transitions

**Design:**
- Toast variants: `success`, `error`, `info` (map to design tokens: `--color-success`, `--color-danger`, `--color-info`)
- Each toast: icon (Lucide: `CheckCircle`, `XCircle`, `Info`), message text, auto-dismiss (default 4s), manual close button
- Auto-dismiss progress bar: a thin colored bar at the bottom of each toast that shrinks over the dismiss duration (CSS animation on `width` from 100% to 0%), giving visual feedback of time remaining. Color matches the toast variant.
- Position: top-right, stacked vertically with gap
- Transitions: slide-in from right, fade-out
- Mount `ToastContainer` in `App.vue` so it's available app-wide

**Pattern reference:** Follow the composable pattern from `web/reflekt-web/src/composables/useSprintHub.ts` for the composable structure.

### Step 2: Phase Indicator Stepper

Create a horizontal stepper component showing the current retro phase.

**Files to create:**
- `web/reflekt-web/src/components/PhaseIndicator.vue` — horizontal stepper with 4 steps: Writing, Voting, Discussing, Reporting

**Design:**
- Steps rendered as a horizontal row with connecting lines between them
- Each step: circle with step number or icon (Lucide: `PenLine`, `ThumbsUp`, `MessageSquare`, `FileText`), label below
- States: completed (filled primary color + checkmark), current (ring + primary fill, slightly larger/emphasized), upcoming (gray outline)
- The component receives `currentPhase: string` as a prop
- Phase order is hardcoded: `['Writing', 'Voting', 'Discussing', 'Reporting']`
- Transition: when phase changes, animate the step circle fill (CSS transition on background-color and border-color)
- Responsive: on mobile (`< sm`), show abbreviated labels or icons-only

### Step 3: Card Redesign (CardItem.vue)

Overhaul CardItem to use sticky-note aesthetic and adopt shared components.

**Files to modify:**
- `web/reflekt-web/src/components/CardItem.vue`

**Changes:**
- Replace raw `div` wrapper with a styled card that has: `rounded-[--radius-md]` (8px), `shadow-card`, a subtle column-color tint background (new prop `columnColor: 'went-well' | 'didnt-go-well' | 'action-items'`)
- Card background: use the light semantic color as a very subtle tint (e.g., `bg-went-well-light/30` for Went Well cards) instead of plain white
- Replace raw edit/save/cancel buttons with `BaseButton` (variant: `primary` size `sm` for Save, `secondary` size `sm` for Cancel, `ghost` size `sm` for Edit/Delete)
- Replace raw text inputs with `BaseInput`
- Replace emoji characters (`🔗`, `✕`) with Lucide icons (`Link`, `X`)
- Replace `×` remove-link button with `X` icon from Lucide
- Replace assignee raw `span` with `BaseBadge variant="info"`
- Add hover state: `transition-shadow hover:shadow-card-hover` for subtle lift
- Ensure the blurred state still works (blur-md on content, no tint leak)

### Step 4: Column Layout Redesign

Redesign the three-column board layout with enhanced headers, card count badges, column icons, and the new card input area.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**Changes to column headers:**
- Each column header gets: Lucide icon (`SmilePlus` for Went Well, `Frown` for Didn't Go Well, `ListChecks` for Action Items), title text, `BaseBadge` showing card count
- Use semantic column colors from design tokens for the header background (e.g., `bg-went-well-light` with `text-went-well` for the icon/title)
- Better typography: `text-base font-semibold` for column title

**Changes to card input area:**
- Replace raw `input` + `button` with `BaseInput` + `BaseButton`
- Use column-appropriate `BaseButton` variant (primary with column accent for Add button)

**Changes to participants bar:**
- Replace raw participant pills with `BaseBadge variant="primary"` for each participant
- Use `BaseCard` for the participants container

**Changes to page header:**
- Insert `PhaseIndicator` component (from Step 2) between the page title and the board
- Pass `currentPhase` computed value to it

**Changes to vote budget display:**
- Style the vote budget as a `BaseBadge` with `variant="info"`

### Step 5: Vote Button Redesign

Replace the text-triangle vote button with a proper icon-based design.

**Files to modify:**
- `web/reflekt-web/src/components/CardItem.vue`

**Changes:**
- Replace `▲` text with Lucide `ChevronUp` or `ArrowBigUp` icon (16px)
- Replace `−` retract text with Lucide `Minus` icon inside a small button
- Vote count: display as `BaseBadge variant="default"` or inline styled number
- When user has voted on this card: icon fills with primary color, badge shows "N mine" in primary color
- Add CSS transition on the vote count number change (brief scale pulse via `transition: transform 0.15s`)
- Disabled state (no budget, no own votes): muted icon + `cursor-not-allowed`

### Step 6: Card Animations (TransitionGroup)

Add enter/leave animations for cards in each column.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`
- `web/reflekt-web/src/assets/main.css` (add transition classes)

**Changes to RetroRoomView.vue:**
- Wrap each column's card list in `<TransitionGroup name="card-list" tag="div">` instead of a plain `div`
- Each `CardItem` already has `:key="card.id"` which TransitionGroup needs

**CSS transitions to add in main.css:**
```
.card-list-enter-active { transition: all 0.3s ease; }
.card-list-leave-active { transition: all 0.2s ease; position: absolute; }
.card-list-enter-from { opacity: 0; transform: translateY(-10px); }
.card-list-leave-to { opacity: 0; transform: translateX(20px); }
.card-list-move { transition: transform 0.3s ease; }
```

The `TransitionGroup` `tag="div"` must carry the same flex-col + gap classes the current wrapper div has. The leave-active `position: absolute` is needed so remaining cards animate smoothly into place.

### Step 7: Loading State

Add a loading screen while the room is being entered.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**Changes:**
- Add a new reactive `loading` ref (default `true`)
- In `enterRoom()`, set `loading = false` after all fetches + hub connect complete
- In the template, add a third top-level state: `v-if="kicked"` / `v-else-if="loading || !joined"` shows either the join form or a loading state
- When `joined` is false and there's no guest token attempt in progress, show the join form
- When loading (token found, fetching data): show `LoadingSpinner size="lg"` centered with text "Joining retro..."
- The logic: `onMounted` sets loading while auto-join is attempted. If auto-join fails or no token, loading clears and join form shows.

### Step 8: Join Form Branding

Enhance the join form with better visuals and shared component adoption.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**Changes:**
- Wrap join form in `BaseCard` with `padding="lg"`
- Add Reflekt branding: small `RefreshCw` icon (matching AppHeader) + "Reflekt" text above the form
- Sprint name: display prominently as `text-lg font-semibold text-gray-800`
- Participant list: show avatars as initial-letter circles (like AppHeader's user button) using `w-8 h-8 rounded-full bg-primary-100 text-primary-700 font-semibold text-sm flex items-center justify-center` — one per participant from `summaryParticipants`
- Replace raw `input` with `BaseInput`
- Replace raw `button` with `BaseButton variant="primary" size="lg"` with full width
- Error message: use `text-danger` (design token) instead of `text-red-600`

### Step 9: Card Delete Confirmation Modal

Add a confirmation dialog before deleting a card.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**Changes:**
- Add reactive state: `deleteTarget: ref<Card | null>(null)`
- Rename current `confirmDelete` to `requestDelete` — it now sets `deleteTarget` instead of immediately deleting
- Add `executeDelete()` that runs the actual optimistic delete logic (current `confirmDelete` body) and clears `deleteTarget`
- Add `<BaseModal>` at the bottom of the template:
  - `open="!!deleteTarget"`, `title="Delete Card"`
  - Body: "Are you sure you want to delete this card? This action cannot be undone."
  - Footer: two buttons — `BaseButton variant="secondary"` "Cancel" (clears deleteTarget) and `BaseButton variant="danger"` "Delete" (calls executeDelete)
- CardItem `@delete` event now triggers `requestDelete`

### Step 10: Responsive Layout

Make the retro board work across mobile, tablet, and desktop.

**Files to modify:**
- `web/reflekt-web/src/views/RetroRoomView.vue`

**Changes:**

**Mobile (< 640px / default):**
- Replace the grid with a tab-based view: tabs for "Went Well", "Didn't Go Well", and "Action Items" (when visible)
- Tab bar: horizontal row of buttons at the top of the board area, styled with column colors. Active tab: filled background. Inactive: outline/ghost.
- Only the active tab's column renders (v-show for performance — keeps card state alive)
- Add reactive `activeTab` ref, default to first column

**Tablet (sm: 640px — lg: 1024px):**
- 2-column grid: Went Well + Didn't Go Well side by side, Action Items full-width below (when visible)
- `sm:grid sm:grid-cols-2 sm:gap-4`

**Desktop (lg: 1024px+):**
- 3-column grid as current (or 2-column when no Action Items)
- `lg:grid-cols-3` (or `lg:grid-cols-2` when no Action Items)

**PhaseIndicator responsive:** Already handled in Step 2.

**Column max-height:** Change from `max-h-[60vh]` to a responsive value — on mobile the single column can use more vertical space (`max-h-[70vh]`), on desktop keep `max-h-[60vh]`.

**Tab state:** When phase changes and Action Items column appears/disappears, ensure `activeTab` stays valid.

## Migration Notes

None. No backend or database changes.

## Testing Strategy

Each step should be verified by running the dev server and checking in a browser:

1. **Toast system:** Trigger error conditions (e.g., join with taken name) and verify toast appears, auto-dismisses, and can be manually closed.
2. **Phase indicator:** Join a retro, verify current phase is highlighted. Have admin advance phase, verify indicator updates via SignalR.
3. **Card redesign:** Verify cards render with column-color tints, buttons work, edit/delete flows function, evidence links display correctly, blurred cards still blur.
4. **Column headers:** Verify icons, card counts update as cards are added/removed.
5. **Vote button:** Cast and retract votes, verify icon changes and count animates.
6. **Card animations:** Add and remove cards, verify smooth enter/leave transitions and list reflow.
7. **Loading state:** Clear guest token, refresh — verify spinner shows then join form appears. With valid token, verify spinner then room.
8. **Join form:** Verify branding, participant avatars, shared component styling.
9. **Delete confirmation:** Click delete on a card, verify modal appears. Cancel should dismiss. Delete should remove card.
10. **Responsive:** Test at 375px (mobile), 768px (tablet), 1280px (desktop). Verify tab switching on mobile, 2-col on tablet, 3-col on desktop.

## Open Questions

None. All information needed is available from the codebase and research.

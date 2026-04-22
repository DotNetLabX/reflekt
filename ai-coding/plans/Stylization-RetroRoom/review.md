# RetroRoom Stylization — Review

## Verdict: APPROVE

*(Updated after Cycle 1 fixes — all HIGH and MEDIUM issues resolved)*

---

## Cycle 1 Re-review

All four findings from the initial review are confirmed fixed:

1. **[HIGH] maxlength string→number** — All 5 `BaseInput` call sites now use bound syntax (`:maxlength="N"`). Verified by grep.
2. **[HIGH] Toast progress bar positioning** — `relative overflow-hidden` added to per-toast card div at `ToastContainer.vue:8`. Verified by file read.
3. **[MEDIUM] cardBgClass reactivity** — Wrapped in `computed()` with `computed` imported. Verified at `CardItem.vue:207`.
4. **[MEDIUM] isColumnVisible reactivity** — `isMobile` reactive ref at `RetroRoomView.vue:355`; `onResize()` wired to `addEventListener` in `onMounted` (line 495) and `removeEventListener` in `onUnmounted` (line 529). Verified by grep.

**Build evidence:** `npm run type-check` exits 0. `npm run build` succeeds: 1813 modules transformed, built in 638ms, no errors or warnings.

---

## Original Review (Cycle 0)

---

## Pre-commitment Predictions

| Prediction | Actual |
|---|---|
| Type errors from string vs number prop mismatches | CONFIRMED — 5 TS errors |
| Toast progress bar positioning bug | CONFIRMED — absolute child in non-relative parent |
| `isColumnVisible` not reactive to window resize | CONFIRMED — gap, not bug |
| `cardBgClass` not reactive to prop changes | CONFIRMED — computed at setup-time |
| Missing keyboard accessibility on modal | Not an issue — BaseModal handles it |

---

## Findings

### [HIGH] TypeScript type-check fails — 5 errors

**Files:**
- `web/reflekt-web/src/components/CardItem.vue:21,27`
- `web/reflekt-web/src/views/RetroRoomView.vue:44,159,213,266`

**Issue:** `BaseInput` declares `maxlength?: number` but all call sites pass unbound string literals (`maxlength="2000"`, `maxlength="50"`). Vue's template compiler passes these as `string` — TypeScript rejects them with `Type 'string' is not assignable to type 'number'`.

The `textarea` in `CardItem.vue:21` also has `maxlength="2000"` — this is a native HTML attribute so TypeScript accepts `string` there, but line 27 is a `BaseInput` and fails.

**Fix:** Change all `BaseInput` call sites to bound syntax:
```
:maxlength="2000"   (not  maxlength="2000")
:maxlength="50"     (not  maxlength="50")
```

Affected lines: `CardItem.vue:27`, `RetroRoomView.vue:44,159,213,266`.

---

### [HIGH] Toast progress bar `position: absolute` inside non-relative container

**File:** `web/reflekt-web/src/components/ui/ToastContainer.vue:19`

**Issue:** The progress bar div has `class="absolute bottom-0 left-0 h-0.5 ..."`. Its nearest positioned ancestor is the outer `<div class="fixed top-4 right-4 ...">` wrapper — not the individual toast card div. The toast card div at line 6 has no `relative` class, so the bar escapes the card boundaries and positions relative to the fixed container instead.

**Fix:** Add `relative` to the toast card div (line 6):
```html
<div
  v-for="toast in toasts"
  :key="toast.id"
  class="relative pointer-events-auto flex items-start gap-3 ..."
>
```

---

### [MEDIUM] `cardBgClass` evaluated once at setup-time — not reactive to `columnColor` prop changes

**File:** `web/reflekt-web/src/components/CardItem.vue:207`

**Issue:**
```ts
const cardBgClass = props.columnColor ? cardBgClasses[props.columnColor] ?? 'bg-white' : 'bg-white'
```
This is a plain constant assigned once at component setup, not a `computed()`. If `columnColor` ever changes (e.g. Vue reuses the component with a different prop in v-for), the class won't update.

In practice `columnColor` is a static string per card so this likely won't manifest, but it violates Vue reactivity conventions and will confuse future readers.

**Fix:**
```ts
const cardBgClass = computed(() =>
  props.columnColor ? cardBgClasses[props.columnColor] ?? 'bg-white' : 'bg-white'
)
```

---

### [MEDIUM] `isColumnVisible` reads `window.innerWidth` without reactivity — tab switching may not work on desktop after resize

**File:** `web/reflekt-web/src/views/RetroRoomView.vue:384-387`

**Issue:**
```ts
function isColumnVisible(column: string): boolean {
  if (typeof window !== 'undefined' && window.innerWidth >= 640) return true
  return activeTab.value === column
}
```
`window.innerWidth` is read at call time but is not reactive. If a user opens at < 640px (mobile), then resizes to >= 640px without a full page reload, the function still returns `activeTab.value === column` because `window.innerWidth` is not a reactive signal — Vue won't re-evaluate this when the window resizes.

The reverse (desktop → mobile) also breaks: all three columns start `v-show="true"`, then after resize they'd all stay visible until `activeTab` changes.

**Fix:** Use a reactive breakpoint composable or `useEventListener('resize', ...)` to track `isMobile`. This is a medium because the most common case (fixed viewport on mobile device) works fine; only browser resize of a desktop window triggers it.

---

## Positive Observations

- Module-level toast state pattern in `useToast.ts` is clean and correct — matches `useSprintHub` reference pattern with the intended singleton behavior.
- `PhaseIndicator.vue` handles the edge case where `currentPhase` doesn't match any known phase name (returns index 0) — defensive and correct.
- `executeDelete()` correctly clears `deleteTarget` before the async call, preventing a double-delete if the user clicks rapidly.
- `watch(showActionItems, ...)` that resets `activeTab` to `'WentWell'` when Action Items disappear is a correct edge case guard.
- All Lucide icon replacements are correct (CheckCircle/XCircle/Info for toasts, PenLine/ThumbsUp/MessageSquare/FileText for phase steps, SmilePlus/Frown/ListChecks for columns, ChevronUp/Minus for voting).
- `TransitionGroup` uses `relative` on the wrapper and `position: absolute` on `leave-active` in CSS — correct pattern for smooth list reflows.
- `BaseModal` is used correctly with `@close="deleteTarget = null"` wired properly.

---

## Gaps

- No test for the toast progress bar visual — only discoverable at runtime.
- Responsive tab behavior is not tested for the window-resize edge case.
- The `textarea` in `CardItem.vue:17-23` (used for card editing) is not replaced with `BaseInput` — plan step 3 says "Replace raw text inputs with `BaseInput`" but the edit textarea remains a raw `<textarea>`. This is a valid exception since `BaseInput` wraps `<input>` not `<textarea>`, but the plan doesn't explicitly call it out. Noting as a gap rather than a finding.

---

## Open Questions

- None after self-audit. All HIGH findings are confirmed with direct file:line evidence.

---

## Evidence

| Check | Result | Command | Output |
|---|---|---|---|
| Backend build | FAIL (file lock, not compile error) | `dotnet build Reflekt.API.csproj` | MSB3027: DLLs locked by running Reflekt.API (PID 2176) — not a code error |
| Frontend type-check | FAIL | `npm run type-check` | 5 TS2322 errors in CardItem.vue and RetroRoomView.vue |
| Vite build | Not run (type-check failed first) | — | — |

---

## Cross-Validation (Codex)

### Agreed (both Sonnet and Codex flagged)
- **maxlength string vs number** — CardItem.vue:27, RetroRoomView.vue:44,159,213,266 — use `:maxlength="N"` bound syntax
- **Toast progress bar absolute positioning** — ToastContainer.vue:18 — toast card div missing `relative` class
- **`cardBgClass` not reactive** — CardItem.vue:207 — should be `computed()`
- **`isColumnVisible` not reactive to resize** — RetroRoomView.vue:384 — reads `window.innerWidth` once, no resize listener

### Sonnet only
- None (all Sonnet findings were confirmed by Codex)

### Codex only
- **LOW** — Evidence-link URL input (CardItem.vue:114) is a raw `<input>` not replaced with `BaseInput`. Valid gap but raw input is acceptable here since it's inline/compact and the plan intent was likely the main edit inputs.
- **LOW** — Vote count `transition-transform` at CardItem.vue:147 has no actual transform state change, so the pulse animation won't fire. The `transition-transform` class is present but the `:key="voteCount"` on the span doesn't trigger a CSS transform — it only causes Vue to re-render the element. A `@keyframes` scale pulse would be needed to actually animate.

### Disagreements
- Codex rates the `maxlength` type mismatch as **Medium**; Sonnet rates it **HIGH** because `vue-tsc` produces hard errors that block CI. The build evidence confirms HIGH is correct.
- Codex rates the toast positioning as **Medium**; Sonnet rates it **HIGH** because the progress bar is visually broken for all users — the bar anchors to the outer fixed container, not each individual card. HIGH is the correct severity given it affects every toast shown.

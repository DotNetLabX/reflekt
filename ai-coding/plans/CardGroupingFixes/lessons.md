# Card Grouping Fixes — Lessons

## Planner Lessons
- When a plan simplifies a UX interaction (e.g., "remove-only on card drop instead of remove+add"), explicitly call it out as a known UX trade-off in the plan step rather than burying it in a paragraph. The reviewer flagged this as a MEDIUM finding because the deviation wasn't immediately obvious from the plan text.
- For frontend-only bug fix plans, consider combining closely related steps that all modify the same template sections (e.g., "make draggable" + "add drop targets" + "add drag-out") into fewer steps. The executor combined steps 2/3/4 anyway — the plan's step ordering was good for logical clarity but over-separated for practical implementation.

## Executor Lessons

- CardGroupItem.vue uses two different voting UI patterns: solo groups use the CardItem-style ChevronUp/Minus controls while multi-card groups use ThumbsUp/ThumbsDown. This inconsistency exists because solo groups must visually match CardItem per business rule 14. Future plans touching voting UI should note this split.
- HTML5 Drag and Drop `dataTransfer` cannot be read during `dragover` in Firefox (security restriction). The `onDragOver` handler must catch and ignore the error, allowing the drop by calling `preventDefault` anyway. This is a browser-specific behavior not documented in the codebase.
- When combining steps that modify the same Vue component template (Steps 2-4 all touch CardGroupItem.vue), implementing them together avoids merge conflicts and redundant template restructuring. The plan's step ordering was good for logical separation but practical implementation benefited from batching.

## Skill Gaps

- **Missing skill:** `create-vue-component` — a skill for creating/modifying Vue 3 components following the project's patterns (script setup, defineProps/defineEmits typing, computed/ref conventions, Tailwind class patterns). Reference files: `CardItem.vue`, `CardGroupItem.vue`, `PhaseIndicator.vue`.

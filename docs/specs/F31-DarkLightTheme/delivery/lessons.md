# Dark / Light Theme — Lessons

## Architect Lessons

- The `tailwind-theme` skill's two-layer pattern (raw vars in `:root`/`.light` + `@theme` aliases) is the right approach for this codebase. The skill already documents flash prevention via `<body style>`, but the spec requires a stronger guarantee (blocking `<script>` that sets the class on `<html>`). The skill could be extended with a "theme initialization" section covering the blocking script pattern.
- Semantic badge tokens (success/danger/warning/info/default/primary) are a reusable pattern that should exist as first-class tokens in any themed app. The spec drove their creation but they will be reusable across future features. Consider documenting this in the tailwind-theme skill.
- When planning a large token migration (200+ references across 37 files), grouping by component layer (base UI -> layout -> feature -> views) gives the developer a natural order where each layer builds on the previous. Base UI components are the foundation -- once they use tokens, downstream components inherit correct behavior through composition.

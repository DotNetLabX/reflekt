# Dark Theme Contrast & Visual Hierarchy — Lessons

## Architect Lessons
- When F31's spec committed to WCAG AA compliance (BR6), the specific token values should have been validated against contrast ratios before shipping. A post-launch contrast audit would have caught the muted text failure. Future theme specs should include a contrast ratio validation step in the acceptance criteria — not just "meets WCAG AA" but specific ratio calculations per token against each surface it appears on.
- Theme-aware badge tokens (`--badge-{variant}-text`) are useful beyond badges — they provide "readable color X on current surface" values. When phase labels or other non-badge elements need the same dual-value behavior, reusing badge tokens is a pragmatic BR9-compliant approach. This pattern should be documented if it recurs.
- The `claude --print` background command approach for spawning a critic review did not produce output. Need to investigate whether this is a Windows/PowerShell path issue or a CLI limitation. For now, self-review was effective for a scoped 5-step plan.

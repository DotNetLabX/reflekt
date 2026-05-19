# Timer per Phase -- Lessons

## Architect Lessons
- The spec is at `docs/features/TimerPerPhase/spec.md` rather than the standard `docs/specs/{slug}/definition/spec.md` path. Feature specs live in a different location than the convention assumes. This is fine -- the plan references the actual path.
- No skill exists for adding hub methods to an existing SignalR hub. This is a recurring pattern (readiness was added the same way). A `add-hub-method` skill could codify: claim validation, group broadcast, snapshot-on-join pattern.
- No skill exists for ephemeral in-memory singleton services (like TimerService or the ReadinessState pattern in SprintHub). A `create-ephemeral-state-service` skill could cover: ConcurrentDictionary structure, thread safety patterns, DI registration as singleton, cleanup on dispose.
- The SprintHub already uses static ConcurrentDictionary fields for ReadinessState and ConnectionState. TimerService follows the same pattern but as a proper injected singleton service rather than static fields -- this is a better pattern. Consider refactoring ReadinessState out of SprintHub into a dedicated service in a future cleanup.

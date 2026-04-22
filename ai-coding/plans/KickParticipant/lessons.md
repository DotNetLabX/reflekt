# Kick Participant — Lessons

## Executor Lessons
- The `ParticipantRepository` had no `Remove()` method — unlike `CardRepository` which had one from the start. Repositories should get CRUD methods as features demand them, not speculatively.
- Cookie auth (SM) and guest token auth (participant) are fully separate identity contexts. Cross-referencing them (e.g., "is this participant the SM?") requires storing shared data (like email) on the participant, which v1 does not do.

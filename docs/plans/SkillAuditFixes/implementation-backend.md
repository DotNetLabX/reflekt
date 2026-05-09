# SkillAuditFixes — Backend Implementation

## Files Created
- `src/Services/Reflekt/Reflekt.API/Mappings/CardMappings.cs` — Mapster IRegister for Card and EvidenceLink mappings
- `src/Services/Reflekt/Reflekt.API/Mappings/SprintMappings.cs` — Mapster IRegister for Sprint mappings
- `src/Services/Reflekt/Reflekt.API/Mappings/ParticipantMappings.cs` — Mapster IRegister for Participant mappings
- `src/Services/Reflekt/Reflekt.API/Mappings/CardGroupMappings.cs` — Mapster IRegister for CardGroup and Card→CardGroupCardEntry mappings
- `src/Services/Reflekt/Reflekt.API/Mappings/ReportMappings.cs` — Mapster IRegister for Report mappings

## Files Modified

### Step 1 — Skill definitions (backend)
- `.claude/skills/create-aggregate/workflows/AggregateEfCore.md` — Changed AuditedEntityConfiguration→EntityConfiguration, updated file naming, removed stale overrides, added composite-key note
- `.claude/skills/persistence-patterns/SKILL.md` — Removed entire AuditedEntityConfiguration section, replaced with actual EntityConfiguration usage, added SQLite and child entity notes

### Step 2 — Skill definitions (frontend/feature)
- `.claude/skills/create-feature/workflows/Mappings.md` — Added FastEndpoints variant path, removed Application-only path
- `.claude/skills/create-vue-feature/SKILL.md` — Added PageLayout/PageToolbar prerequisite note, updated folder structure

### Step 3 — Aggregate property accessors
- `src/Services/Reflekt/Reflekt.Domain/Cards/Card.cs` — SprintId, ParticipantId, ColumnType, CardType, CreatedAt → required init
- `src/Services/Reflekt/Reflekt.Domain/Participants/Participant.cs` — SprintId, DisplayName, GuestToken, JoinedAt → required init
- `src/Services/Reflekt/Reflekt.Domain/Votes/Vote.cs` — GroupId, ParticipantId, CreatedAt → required init
- `src/Services/Reflekt/Reflekt.Domain/CardGroups/CardGroup.cs` — SprintId, ColumnType, CreatedAt → required init
- `src/Services/Reflekt/Reflekt.Domain/Cards/EvidenceLink.cs` — CardId, Url, Source → required init

### Step 4 — Mapster IRegister conversion
- `src/Services/Reflekt/Reflekt.API/GlobalUsings.cs` — Added global using Mapster
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — Added TypeAdapterConfig.GlobalSettings.Scan assembly scan
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CardContracts.cs` — Removed static CardMappings class and unused using
- `src/Services/Reflekt/Reflekt.API/Features/Sprints/SprintContracts.cs` — Removed static SprintMappings class and unused using
- `src/Services/Reflekt/Reflekt.API/Features/Participants/ParticipantContracts.cs` — Removed static ParticipantMappings class and unused using
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/CardGroupContracts.cs` — Removed static CardGroupMappings class and unused usings
- `src/Services/Reflekt/Reflekt.API/Features/Reports/ReportContracts.cs` — Removed static ReportMappings class and unused using
- 23 endpoint/handler files — Replaced {Domain}Mappings.ToResponse() calls with .Adapt<T>() and `with` expressions

### Step 6 — Seal HealthEndpoint
- `src/Services/Reflekt/Reflekt.API/Features/Health/HealthEndpoint.cs` — Added sealed keyword

## Key Decisions
- Added `Card → CardGroupCardEntry` config to CardGroupMappings IRegister to simplify multi-param call sites
- Used `global using Mapster;` in GlobalUsings.cs instead of per-file usings
- Behavior files unchanged — Create factory methods already used object initializer syntax compatible with `required init`
- Followed Team/Sprint pattern for `required init` — private constructor without `[SetsRequiredMembers]` (EF Core handles via reflection)

## Deviations from Plan
- None

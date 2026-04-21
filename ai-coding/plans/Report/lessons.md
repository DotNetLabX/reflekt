# Report — Lessons

## Planner Lessons
- Plan Step 2 described auth check and data loading but did not explicitly sequence them. Reviewer caught that auth verification must complete before loading cards/votes/participants (the expensive queries). The plan listed steps 3-7 as a flat list without ordering constraints. Future plans for dual-auth endpoints should explicitly state: "verify access first, then load data" as a sequencing requirement.
- The plan did not call out the need for a static mappings class in ReportContracts.cs, even though the pattern reference (CardContracts.cs) includes CardMappings. Reviewer flagged this as MEDIUM. When referencing a pattern file, explicitly list which structural elements from that pattern to replicate.

## Executor Lessons
- No .sln file exists at the repo root; build using the .csproj path directly: `dotnet build src/Services/Reflekt/Reflekt.API/Reflekt.API.csproj`
- vue-tsc --noEmit produces no output on success (exit code 0, empty stdout)

## Skill Gaps
- No gaps encountered. The create-feature skill targets full CQRS slices; this read-only projection endpoint was simpler and correctly handled by following ListCardsEndpoint directly.

# ExceptionInfrastructure — Implementation

## Files Created

- `src/BuildingBlocks/Blocks.Domain/DomainException.cs` — base class for domain rule violations; primary constructor pattern
- `src/BuildingBlocks/Blocks.Exceptions/Blocks.Exceptions.csproj` — no dependencies, pure exception types
- `src/BuildingBlocks/Blocks.Exceptions/HttpException.cs` — base HTTP-mapped exception with StatusCode property
- `src/BuildingBlocks/Blocks.Exceptions/BadRequestException.cs` — 400 exception
- `src/BuildingBlocks/Blocks.Exceptions/NotFoundException.cs` — 404 exception
- `src/BuildingBlocks/Blocks.Exceptions/UnauthorizedException.cs` — 401 exception
- `src/BuildingBlocks/Blocks.Exceptions/Extensions.cs` — `SingleOrThrow` LINQ extension in `Blocks.Linq` namespace (matches reference project convention)
- `src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj` — references Blocks.Exceptions
- `src/BuildingBlocks/Blocks.Core/Guard.cs` — static guard utilities (ThrowIfNullOrWhiteSpace, NotFound, AgainstNull, etc.)
- `src/BuildingBlocks/Blocks.Core/GuardExtensions.cs` — `OrThrowNotFound` extension method
- `src/BuildingBlocks/Blocks.Persistence/Extensions/RepositoryExtensions.cs` — async guard extensions for repository lookups (FindByIdOrThrowAsync, GetByIdOrThrowAsync, ExistsOrThrowAsync, EnsureNotExistsOrThrowAsync)
- `src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs` — catches ValidationException, OperationCanceledException, and all other exceptions; maps to appropriate HTTP status codes; returns JSON with StatusCode, Message, TraceId, Details

## Files Modified

- `src/BuildingBlocks/Blocks.Domain/Entity.cs` — namespace `BuildingBlocks.Domain` → `Blocks.Domain`
- `src/BuildingBlocks/Blocks.Domain/AggregateRoot.cs` — namespace updated
- `src/BuildingBlocks/Blocks.Domain/IDomainEvent.cs` — namespace updated
- `src/BuildingBlocks/Blocks.Domain/IHasDomainEvents.cs` — namespace updated
- `src/BuildingBlocks/Blocks.Persistence/ApplicationDbContext.cs` — namespace + using updated
- `src/BuildingBlocks/Blocks.Persistence/DomainEventPublishingInterceptor.cs` — namespace + using updated
- `src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs` — namespace + using updated; added `Query()`, `FindByIdAsync()`, `ExistsAsync()` methods
- `src/BuildingBlocks/Blocks.Persistence/EntityConfigurations/EntityConfiguration.cs` — namespace + using updated
- `src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj` — ProjectReference updated to `Blocks.Domain`; added ProjectReference to `Blocks.Core`
- `src/Services/Reflekt/Reflekt.Domain/GlobalUsings.cs` — `global using BuildingBlocks.Domain` → `global using Blocks.Domain`
- `src/Services/Reflekt/Reflekt.Domain/**/*.cs` (24 files) — `using BuildingBlocks.Domain` → `using Blocks.Domain` via PowerShell bulk replace
- `src/Services/Reflekt/Reflekt.Domain/Reflekt.Domain.csproj` — ProjectReference updated to `Blocks.Domain`
- `src/Services/Reflekt/Reflekt.Persistence/GlobalUsings.cs` — `global using BuildingBlocks.Persistence` → `global using Blocks.Persistence`
- `src/Services/Reflekt/Reflekt.Persistence/Reflekt.Persistence.csproj` — ProjectReference updated to `Blocks.Persistence`
- `src/Services/Reflekt/Reflekt.Persistence/*.cs` and `EntityConfigurations/*.cs` (10 files) — usings updated via PowerShell bulk replace
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs` — removed `AddProblemDetails()`; replaced dev/prod exception handler block with `UseMiddleware<GlobalExceptionMiddleware>()`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs` — removed try/catch around `Card.Create`; exception propagates to middleware
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs` — removed try/catch around `card.Update`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs` — removed try/catch around `group.SetTitle`
- `tests/Reflekt.UnitTests/Reflekt.UnitTests.csproj` — ProjectReference updated to `Blocks.Domain`
- `tests/Reflekt.UnitTests/BuildingBlocksTests/AggregateRootTests.cs` — `using BuildingBlocks.Domain` → `using Blocks.Domain`
- `Reflekt.slnx` — updated project paths for renamed packages; added `Blocks.Exceptions` and `Blocks.Core`
- `src/Services/Reflekt/CLAUDE.md` — updated BuildingBlocks references to `Blocks.Domain`, `Blocks.Persistence`

## Key Decisions

- **Bulk namespace replace via PowerShell:** Rather than editing 24+ domain files individually, used `Get-ChildItem | Set-Content` with regex replace. Safer and faster than per-file edits.
- **Transitive references:** `Reflekt.API` accesses `Blocks.Exceptions` and `Blocks.Domain` transitively through its existing project references — no new `ProjectReference` needed in `Reflekt.API.csproj`.
- **`FindByIdAsync` vs `GetByIdAsync`:** Both use `FindAsync` in the current codebase. Added `FindByIdAsync` as an alias per plan; `RepositoryExtensions` uses it for `FindByIdOrThrowAsync` and `GetByIdAsync` for `GetByIdOrThrowAsync`.
- **Reference project unavailable:** `D:\src\dotnet-microservices` was not accessible. Proceeded with plan-provided signatures, which were complete and unambiguous.

## Deviations from Plan

- **Step 7 (skill updates) partially completed:** The `error-handling` SKILL.md could not be updated — both Edit and Write tools were denied permission for `.claude/skills/` files. The skill's middleware path (`Blocks.AspNetCore/Middlewares/`) is still incorrect. All other steps completed as specified. User must manually update `.claude/skills/error-handling/SKILL.md` or grant write permission.
- **Integration test failures:** 8 integration tests fail (pre-existing, unrelated to this work — auth/phase logic and vote count issues). All 69 unit tests pass.

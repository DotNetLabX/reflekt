# Exception Infrastructure

## Context

The skill audit (`docs/audits/full-skill-audit.md`) found that the error-handling skill documents a full exception stack (exception hierarchy, GlobalExceptionMiddleware, Guard utilities) that doesn't exist in the Reflekt codebase. The reference implementation lives in `D:\src\dotnet-microservices` under `Blocks.Exceptions`, `Blocks.Core`, `Blocks.Domain`, and `Blocks.AspNetCore`.

Additionally, the existing BuildingBlocks packages in Reflekt use the wrong naming convention (`BuildingBlocks.{Name}` instead of `Blocks.{Name}`). This plan corrects the naming and adds the missing exception infrastructure.

Currently, Reflekt uses ASP.NET Core's built-in `UseExceptionHandler()` + `AddProblemDetails()`, which returns generic 500s for unhandled exceptions. Three endpoints manually catch `ArgumentException` to avoid this.

## Scope

**In scope:**
- Rename existing `BuildingBlocks.Domain` → `Blocks.Domain` and `BuildingBlocks.Persistence` → `Blocks.Persistence`
- Create `Blocks.Exceptions` package (HttpException hierarchy)
- Add `DomainException` to `Blocks.Domain`
- Create `Blocks.Core` package (Guard utilities)
- Add `RepositoryExtensions` to `Blocks.Persistence`
- Create `GlobalExceptionMiddleware` in Reflekt.API
- Wire up middleware and remove endpoint try/catches
- Update skills to be project-agnostic (no hardcoded project names)

**Out of scope:**
- Creating a `Blocks.AspNetCore` package (single-service project — middleware lives in Reflekt.API for now; extract to Blocks.AspNetCore when a second service is added)
- Migrating all domain `ArgumentException` guards to `DomainException` (follow-up)
- Adding `ConflictException` / `ForbiddenException` (extend when needed per the skill)

## Domain Model Changes

- `DomainException` added to `Blocks.Domain` — base class for domain rule violations

## Data Model Changes

None.

## Skill Mapping

| Step | Skill | Disposition |
|------|-------|-------------|
| 0 | None | Rename existing BuildingBlocks packages to Blocks.{Name} |
| 1 | create-building-blocks-package | Scaffold Blocks.Exceptions |
| 2 | create-building-blocks-package | Scaffold Blocks.Core |
| 3 | None (existing project) | Add DomainException to Blocks.Domain |
| 4 | persistence-patterns | Add RepositoryExtensions to Blocks.Persistence |
| 5 | error-handling | Create GlobalExceptionMiddleware |
| 6 | None | Wire up and remove try/catches |
| 7 | improve-skills | Update skill definitions — project-agnostic, correct namespaces |

## Implementation Steps

### Step 0 — Rename existing BuildingBlocks packages to Blocks.{Name}

The existing packages use the wrong naming convention. Rename them to match the canonical `Blocks.{Name}` pattern.

**Renames:**

| Current | Target |
|---------|--------|
| `src/BuildingBlocks/BuildingBlocks.Domain/` | `src/BuildingBlocks/Blocks.Domain/` |
| `BuildingBlocks.Domain.csproj` | `Blocks.Domain.csproj` |
| `namespace BuildingBlocks.Domain` | `namespace Blocks.Domain` |
| `src/BuildingBlocks/BuildingBlocks.Persistence/` | `src/BuildingBlocks/Blocks.Persistence/` |
| `BuildingBlocks.Persistence.csproj` | `Blocks.Persistence.csproj` |
| `namespace BuildingBlocks.Persistence` | `namespace Blocks.Persistence` |

**Sub-steps:**

1. Rename folders: `BuildingBlocks.Domain/` → `Blocks.Domain/`, `BuildingBlocks.Persistence/` → `Blocks.Persistence/`
2. Rename csproj files inside each folder
3. Find-and-replace all namespaces across the entire codebase:
   - `BuildingBlocks.Domain` → `Blocks.Domain` (in all `.cs` files, `.csproj` ProjectReference paths)
   - `BuildingBlocks.Persistence` → `Blocks.Persistence` (in all `.cs` files, `.csproj` ProjectReference paths)
4. Update `using` statements in all consuming projects (`Reflekt.Domain`, `Reflekt.Persistence`, `Reflekt.API`)
5. Update solution file references: `dotnet sln remove` old paths, `dotnet sln add` new paths
6. Update EF migration snapshot if it contains namespace references

5. Run `dotnet clean` before renaming to clear stale `obj/` and `bin/` artifacts under old folder names

**Files to modify (exhaustive):**

Source projects:
- `src/BuildingBlocks/BuildingBlocks.Domain/**/*.cs` (all namespace declarations and usings)
- `src/BuildingBlocks/BuildingBlocks.Persistence/**/*.cs`
- `src/Services/Reflekt/Reflekt.Domain/**/*.cs` (usings)
- `src/Services/Reflekt/Reflekt.Persistence/**/*.cs` (usings + csproj ProjectReferences)
- `src/Services/Reflekt/Reflekt.API/**/*.cs` (usings + csproj ProjectReferences)

Test projects:
- `tests/Reflekt.UnitTests/Reflekt.UnitTests.csproj` (ProjectReference to `BuildingBlocks.Domain`)
- `tests/Reflekt.UnitTests/BuildingBlocksTests/AggregateRootTests.cs` (`using BuildingBlocks.Domain`)
- `tests/Reflekt.IntegrationTests/*.csproj` (check for ProjectReferences)

Solution file:
- `Reflekt.slnx` — XML format, edit project paths directly (verify `dotnet sln` supports `.slnx`; if not, edit XML manually)

Documentation:
- `src/Services/Reflekt/CLAUDE.md` — references "BuildingBlocks used: `BuildingBlocks.Domain`, `BuildingBlocks.Persistence`"

EF migrations:
- `src/Services/Reflekt/Reflekt.Persistence/Migrations/*Designer.cs` and snapshot — may contain namespace references

**Verify:** `dotnet build` passes. `dotnet test` passes. Grep for any remaining `BuildingBlocks.Domain` or `BuildingBlocks.Persistence` references across the entire repo (excluding `docs/` which describes historical state).

**Depends on:** Nothing

---

### Step 1 — Create Blocks.Exceptions

Scaffold a new package for HTTP-mapped exception types using the `create-building-blocks-package` skill.

**Csproj:** No dependencies — pure exception types with no framework reference.

**Files to create:**

`src/BuildingBlocks/Blocks.Exceptions/Blocks.Exceptions.csproj`

`src/BuildingBlocks/Blocks.Exceptions/HttpException.cs`:
```csharp
using System.Net;

namespace Blocks.Exceptions;

public class HttpException : Exception
{
    public HttpException(HttpStatusCode statusCode, string? message, Exception ex)
        : base(message, ex)
    {
        HttpStatusCode = statusCode;
    }

    public HttpException(HttpStatusCode statusCode, string? message)
        : base(string.IsNullOrEmpty(message) ? statusCode.ToString() : message)
    {
        HttpStatusCode = statusCode;
    }

    public HttpStatusCode HttpStatusCode { get; }
    public int StatusCode => (int)HttpStatusCode;
}
```

`src/BuildingBlocks/Blocks.Exceptions/BadRequestException.cs`:
```csharp
using System.Net;

namespace Blocks.Exceptions;

public class BadRequestException : HttpException
{
    public BadRequestException(string exceptionMessage)
        : base(HttpStatusCode.BadRequest, exceptionMessage) { }

    public BadRequestException(string exceptionMessage, Exception exception)
        : base(HttpStatusCode.BadRequest, exceptionMessage, exception) { }
}
```

`src/BuildingBlocks/Blocks.Exceptions/NotFoundException.cs`:
```csharp
using System.Net;

namespace Blocks.Exceptions;

public class NotFoundException : HttpException
{
    public NotFoundException(string exceptionMessage)
        : base(HttpStatusCode.NotFound, exceptionMessage) { }

    public NotFoundException(Exception exception, string? exceptionMessage)
        : base(HttpStatusCode.NotFound, exceptionMessage, exception) { }
}
```

`src/BuildingBlocks/Blocks.Exceptions/UnauthorizedException.cs`:
```csharp
using System.Net;

namespace Blocks.Exceptions;

public class UnauthorizedException : HttpException
{
    public UnauthorizedException(string? message)
        : base(HttpStatusCode.Unauthorized, message) { }
}
```

`src/BuildingBlocks/Blocks.Exceptions/Extensions.cs`:
```csharp
// Namespace intentionally differs from project — matches reference project convention.
// LINQ extensions live in Blocks.Linq so they auto-resolve alongside System.Linq usings.
namespace Blocks.Linq;

public static class Extensions
{
    public static TSource SingleOrThrow<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        var item = source.SingleOrDefault(predicate);
        if (item is null) throw new Blocks.Exceptions.NotFoundException($"{typeof(TSource).Name} not found");
        return item;
    }
}
```

**Register:** `dotnet sln add src/BuildingBlocks/Blocks.Exceptions/Blocks.Exceptions.csproj`

**Verify:** `dotnet build`

**Depends on:** Step 0

---

### Step 2 — Create Blocks.Core

Scaffold a new package for Guard utilities.

**Csproj:** References `Blocks.Exceptions`.

**Files to create:**

`src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj` — with `ProjectReference` to `Blocks.Exceptions`

`src/BuildingBlocks/Blocks.Core/Guard.cs`:
```csharp
using Blocks.Exceptions;

namespace Blocks.Core;

public static class Guard
{
    public static void ThrowIfNullOrWhiteSpace(string value)
        => ArgumentException.ThrowIfNullOrWhiteSpace(value);

    public static void ThrowIfNotEqual<T>(T value, T other) where T : IEquatable<T>?
        => ArgumentOutOfRangeException.ThrowIfNotEqual(value, other);

    public static void ThrowIfNull<T>(T? value, string name)
    {
        if (value is null)
            throw new ArgumentNullException(name);
    }

    public static void ThrowIfFalse(this bool condition, string message = "Condition must be true.")
    {
        if (!condition)
            throw new ArgumentException(message);
    }

    public static T AgainstNull<T>(T? value, string parameterName)
        => value ?? throw new ArgumentNullException(parameterName, $"Value cannot be null: '{parameterName}'.");

    public static T NotFound<T>(T? value) where T : class
        => value ?? throw new NotFoundException($"{typeof(T).Name} not found");
}
```

`src/BuildingBlocks/Blocks.Core/GuardExtensions.cs`:
```csharp
using Blocks.Exceptions;

namespace Blocks.Core;

public static class GuardExtensions
{
    public static T OrThrowNotFound<T>(this T? value, string? message = null) where T : class
        => value ?? throw new NotFoundException(message ?? $"{typeof(T).Name} not found");
}
```

**Register:** `dotnet sln add src/BuildingBlocks/Blocks.Core/Blocks.Core.csproj`

**Verify:** `dotnet build`

**Depends on:** Step 1

---

### Step 3 — Add DomainException to Blocks.Domain

Add the domain exception base class to the existing `Blocks.Domain` project (renamed in Step 0). No new project references needed — `DomainException` has no dependency on `Blocks.Exceptions`.

**Files to create:**

`src/BuildingBlocks/Blocks.Domain/DomainException.cs`:
```csharp
namespace Blocks.Domain;

public class DomainException(string message, Exception? innerException = null)
    : Exception(message, innerException);
```

**Verify:** `dotnet build`

**Depends on:** Step 0

---

### Step 4 — Add RepositoryExtensions to Blocks.Persistence

Add async guard extensions for repository lookups. This step has two parts: first extend `RepositoryBase` with missing methods, then add guard extensions.

The current `RepositoryBase` only has `AddAsync`, `SaveChangesAsync`, and `GetByIdAsync`. The reference project's `RepositoryBase` has additional methods (`FindByIdAsync`, `ExistsAsync`, `Query()`) that the extensions need.

**Sub-step 4a — Extend RepositoryBase with missing methods**

Add `FindByIdAsync`, `ExistsAsync`, and `Query()` to `RepositoryBase`. Check the reference project at `D:\src\dotnet-microservices\src\BuildingBlocks\Blocks.EntityFrameworkCore\Repositories\RepositoryBase.cs` for the exact signatures.

**Files to modify:**

`src/BuildingBlocks/Blocks.Persistence/Repositories/RepositoryBase.cs` — add missing methods to the 3-type-param base class:
```csharp
public virtual IQueryable<TEntity> Query()
    => Db.Set<TEntity>().AsQueryable();

public virtual async Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default)
    => await Db.Set<TEntity>().FindAsync([id], ct);

public virtual async Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
    => await Db.Set<TEntity>().FindAsync([id], ct) is not null;
```

Note: `GetByIdAsync` already exists and calls `FindAsync`. `FindByIdAsync` is an alias for clarity — if they would be identical, just keep one and alias via the extensions. Check the reference project to see if `GetByIdAsync` and `FindByIdAsync` differ (e.g., `FindByIdAsync` uses `FindAsync` while `GetByIdAsync` uses `Query().FirstOrDefault` with includes).

**Sub-step 4b — Add RepositoryExtensions**

Add a project reference from `Blocks.Persistence` to `Blocks.Core` (which transitively brings in `Blocks.Exceptions`).

**Files to modify:**

`src/BuildingBlocks/Blocks.Persistence/Blocks.Persistence.csproj` — add `ProjectReference` to `Blocks.Core`

**Files to create:**

`src/BuildingBlocks/Blocks.Persistence/Extensions/RepositoryExtensions.cs`:
```csharp
using Blocks.Core;
using Blocks.Domain;
using Blocks.Exceptions;

namespace Blocks.Persistence;

public static class RepositoryExtensions
{
    public static async Task<TEntity> FindByIdOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id)
        where TContext : DbContext
        where TEntity : Entity<int>
        => Guard.NotFound(await repository.FindByIdAsync(id));

    public static async Task<TEntity> GetByIdOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
        => Guard.NotFound(await repository.GetByIdAsync(id, ct));

    public static async Task ExistsOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
    {
        if (!await repository.ExistsAsync(id, ct))
            throw new NotFoundException($"{typeof(TEntity).Name}({id}) doesn't exist");
    }

    public static async Task EnsureNotExistsOrThrowAsync<TEntity, TContext>(
        this RepositoryBase<TContext, TEntity> repository, int id, CancellationToken ct = default)
        where TContext : DbContext
        where TEntity : Entity<int>
    {
        if (await repository.ExistsAsync(id, ct))
            throw new BadRequestException($"{typeof(TEntity).Name}({id}) already exists");
    }
}
```

Note: Type constraint is `Entity<int>` (the actual base class), not `IEntity<int>` (which doesn't exist in this codebase).

**Verify:** `dotnet build`

**Depends on:** Steps 1, 2

---

### Step 5 — Create GlobalExceptionMiddleware in Reflekt.API

Create the middleware in Reflekt.API (not in a Blocks package — extract to `Blocks.AspNetCore` when a second service is added).

**Files to create:**

`src/Services/Reflekt/Reflekt.API/Middleware/GlobalExceptionMiddleware.cs`:
```csharp
using System.Net;
using System.Text.Json;
using Blocks.Domain;
using Blocks.Exceptions;
using FluentValidation;

namespace Reflekt.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    private static HttpStatusCode MapStatusCode(Exception ex) => ex switch
    {
        ValidationException => HttpStatusCode.BadRequest,
        ArgumentException => HttpStatusCode.BadRequest,
        BadRequestException => HttpStatusCode.BadRequest,
        NotFoundException => HttpStatusCode.NotFound,
        DomainException => HttpStatusCode.BadRequest,
        UnauthorizedException => HttpStatusCode.Unauthorized,
        _ => HttpStatusCode.InternalServerError
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (OperationCanceledException)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = MapStatusCode(exception);
        if (statusCode >= HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

        var response = new
        {
            StatusCode = (int)statusCode,
            exception.Message,
            TraceId = context.TraceIdentifier,
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, statusCode, response);
    }

    private Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var response = new
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "One or more validation errors occurred.",
            TraceId = context.TraceIdentifier,
            Errors = exception.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

**Verify:** `dotnet build`

**Depends on:** Steps 1, 3 (needs Blocks.Exceptions + DomainException)

---

### Step 6 — Wire up middleware and remove endpoint try/catches

**Sub-steps:**

1. In `Reflekt.API/DependencyInjection.cs`:
   - Remove `services.AddProblemDetails();` from `AddReflektApi()` (dead code — the middleware returns its own JSON shape, not ProblemDetails)
   - Replace the entire conditional block:
     ```csharp
     if (app.Environment.IsDevelopment())
         app.UseDeveloperExceptionPage();
     else
         app.UseExceptionHandler();
     ```
     with a single line:
     ```csharp
     app.UseMiddleware<Reflekt.API.Middleware.GlobalExceptionMiddleware>();
     ```
     Place it early in the pipeline (before `UseRouting`). The middleware already includes dev-mode stack traces via `env.IsDevelopment()`, so the developer exception page is not needed.

2. Remove the `try/catch (ArgumentException)` blocks from:
   - `Features/Cards/CreateCard/CreateCardEndpoint.cs` (lines 67-75)
   - `Features/Cards/EditCard/EditCardEndpoint.cs`
   - `Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`

   In each case, remove the try/catch and let the domain method's `ArgumentException` propagate to the middleware, which maps it to 400.

3. Verify the frontend handles the error response shape. The middleware returns:
   ```json
   { "StatusCode": 400, "Message": "...", "TraceId": "...", "Details": null }
   ```
   Check that the Vue stores/API layer reads `Message` from error responses. If the frontend currently parses a different shape (ProblemDetails), update the API fetch error handling.

**Files to modify:**
- `src/Services/Reflekt/Reflekt.API/DependencyInjection.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/CreateCard/CreateCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/Cards/EditCard/EditCardEndpoint.cs`
- `src/Services/Reflekt/Reflekt.API/Features/CardGroups/UpdateGroupTitle/UpdateGroupTitleEndpoint.cs`

**Verify:** `dotnet build`. Test scenarios:
- Create card with empty text → should get 400 with validation message
- Create card with invalid column type → should get 400 from ArgumentException via middleware
- Normal card creation → should work as before

**Depends on:** Step 5

---

### Step 7 — Update skill definitions (project-agnostic)

All skill updates must ensure skills are project-agnostic — use `{ServiceName}`, `{ProjectName}`, `{AggregateName}` placeholders, never hardcoded project names.

**error-handling skill (`.claude/skills/error-handling/SKILL.md`):**
- Verify all paths use `{ServiceName}` placeholders (e.g., `{Svc}.API/Middleware/` not `Reflekt.API/Middleware/`)
- Verify namespaces use `Blocks.*` (already correct)
- Note that `GlobalExceptionMiddleware` lives in `{Svc}.API/Middleware/` until a `Blocks.AspNetCore` package exists
- Add prerequisite: "Requires `Blocks.Exceptions` and `Blocks.Core` packages"
- Scrub any hardcoded project names from the reference project

**create-building-blocks-package skill (`.claude/skills/create-building-blocks-package/SKILL.md`):**
- Naming convention is already correct (`Blocks.{Name}`) — no change needed
- Verify no hardcoded project names

**All other skills that reference BuildingBlocks namespaces:**
- Grep all skills for `BuildingBlocks.` references and update to `Blocks.`
- Grep all skills for hardcoded service/project names and replace with `{ServiceName}` placeholders

**Files to modify:**
- `.claude/skills/error-handling/SKILL.md`
- Any other skills found by grep to contain `BuildingBlocks.` or hardcoded names

**Depends on:** Nothing (can be done in parallel with any step)

---

## Cross-Service Changes

None. Single-service project. When a second service is added:
- Extract `GlobalExceptionMiddleware` to `Blocks.AspNetCore`
- Both services reference the shared middleware

## Migration Notes

No data model changes. EF migration snapshot may contain namespace references to `BuildingBlocks.Persistence` — the rename in Step 0 must update these. If the snapshot breaks, regenerate with `dotnet ef migrations script`.

## Testing Strategy

- **Step 0:** `dotnet build` + `dotnet test` after rename. Grep for any remaining `BuildingBlocks.Domain` or `BuildingBlocks.Persistence` references.
- **Steps 1-4:** `dotnet build` after each step to verify new packages compile
- **Steps 5-6:** `dotnet build` + manual API testing:
  - Invalid input → 400 with message
  - Missing entity → 404 (once endpoints use `Guard.NotFound` / `FindByIdOrThrowAsync`)
  - Valid requests → unchanged behavior
  - Client disconnect → 499 (no crash)
- **Frontend:** Verify Vue API error handling parses the error response shape
- **Unit tests:** Run existing test suite to confirm no regressions

## Resolved Questions

1. **Middleware location:** Reflekt.API for now. Extract to Blocks.AspNetCore when a second service is added.
2. **Naming convention:** `Blocks.{Name}` — existing packages renamed to match.
3. **DomainException migration:** Out of scope. Domain methods currently throw `ArgumentException`, which the middleware handles. Gradual migration to `DomainException` can happen per-aggregate as those files are touched.
4. **Skill hygiene:** Skills must be project-agnostic — no hardcoded project names, use `{ServiceName}` placeholders.

## Skill Gaps Identified

**No new skills needed.** Existing skills cover this work:
- `create-building-blocks-package` — scaffolding (naming already correct)
- `error-handling` — middleware and exception patterns (needs prerequisite note + agnostic scrub)
- `persistence-patterns` — repository extensions

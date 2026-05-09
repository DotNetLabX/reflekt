---
name: create-feature
description: Creates a complete vertical slice feature — endpoint, command/query, handler, validator, mappings, DI. Variant-aware across FastEndpoints, Carter, and Minimal APIs. Check the service CLAUDE.md for which framework to use.
---

# Create Feature

Creates a complete vertical slice for a new feature. The endpoint framework determines which workflow variant to follow.

## Steps

1. **Determine the service and framework.** Check the service's CLAUDE.md for which endpoint framework to use:
   - FastEndpoints → `workflows/EndpointFastEndpoints.md`
   - Carter + MediatR → `workflows/EndpointCarter.md`
   - Minimal APIs + MediatR → `workflows/EndpointMinimalApi.md`

2. **Create the endpoint** using the appropriate workflow above.

3. **Create the command/query and handler** — if the service uses MediatR (Carter/MinAPI), follow `workflows/Handler.md`.

4. **Create the validator** — follow `workflows/Validator.md` (variant-aware).

5. **Create mappings** (if needed) — follow `workflows/Mappings.md`.

6. **Register the endpoint:**
   - FastEndpoints: auto-discovered — no manual registration
   - Carter: auto-discovered via `AddCarter()` — no manual registration
   - Minimal APIs: add to `EndpointRegistration.MapAllEndpoints()` in the API project

7. **Verify the build:** `dotnet build`

## Arguments

Pass the feature name as argument: `/create-feature CreateOrder`

The feature name is used for all file names: `{FeatureName}Endpoint.cs`, `{FeatureName}Command.cs`, etc.

## Feature Folder Structure

### FastEndpoints

Everything co-located in the API project:
```
{Svc}.API/Features/{Domain}/{FeatureName}/
├── {FeatureName}Endpoint.cs           (endpoint + handler logic)
├── {FeatureName}Command.cs            (command + response record + validator, co-located)
```

### Query-Only / No Request DTO

Some endpoints don't have a request body (GET queries, auth flows, DELETE with route-only params). These only need the endpoint file:

```
{Svc}.API/Features/{Domain}/{FeatureName}/
└── {FeatureName}Endpoint.cs           (endpoint + response record if any)
```

- No command file, no validator
- Response record (if any) lives in the endpoint file
- For DELETE with route params: extend `EndpointWithoutRequest` or `Endpoint<TRequest>` binding from route only — no body expected

### Carter + MediatR

Endpoint in API project, handler in Application project:
```
{Svc}.API/Endpoints/{Domain}/
└── {FeatureName}Endpoint.cs           (endpoint, dispatches to MediatR)

{Svc}.Application/Features/{Domain}/{FeatureName}/
├── {FeatureName}Command.cs            (command + response record + validator)
└── {FeatureName}CommandHandler.cs     (MediatR handler)
```

### Minimal APIs + MediatR

Endpoint flat in API project, handler in Application project:
```
{Svc}.API/Endpoints/
└── {FeatureName}Endpoint.cs           (endpoint, dispatches to MediatR)

{Svc}.Application/Features/{FeatureName}/
├── {FeatureName}Command.cs            (command + response record + validator)
└── {FeatureName}CommandHandler.cs     (MediatR handler)
```

Check the service's CLAUDE.md for which framework to use.

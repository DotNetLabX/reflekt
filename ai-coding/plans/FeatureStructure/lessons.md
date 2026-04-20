# FeatureStructure — Lessons

## Developer Lessons

### FastEndpoints HTTP method attribute ambiguity
`FastEndpoints` defines its own `HttpGetAttribute`, `HttpPostAttribute`, etc. in the `FastEndpoints` namespace. When `Microsoft.AspNetCore.Mvc` is also imported (explicitly or implicitly), the compiler raises CS0104 ambiguity. Resolution: do not add `using Microsoft.AspNetCore.Mvc;` in FastEndpoints endpoint files — FastEndpoints' attributes resolve through the global `using FastEndpoints;` already in `GlobalUsings.cs`.

### `git rm` required when deleting tracked files
Deleting files with shell `rm` removes them from disk but leaves them in the git index. `git ls-files` reads the index, so verification checks appear to fail. Use `git rm` (or follow with `git add -u`) to stage the deletions so the index is clean.

### `Directory.CreateDirectory` placement for runtime-created dirs
When removing `.gitkeep` from a directory needed at runtime (e.g. SQLite `data/`), create it unconditionally at app startup — outside any `IsDevelopment()` guard — so it exists in all environments.

## Architect Lessons

### Skill gap: response record location not specified
The `create-feature` skill says `{FeatureName}Command.cs` contains "command + validator, co-located" but doesn't mention where the response record lives. In practice it belongs in the command file (the contract file defines input + output + validation). The skill should be updated to say "command + response + validator, co-located."

### Skill gap: endpoints without a command
The `create-feature` skill assumes every feature has two files (endpoint + command). Endpoints without a request DTO (queries, auth flows) only need the endpoint file — response record (if any) stays in the endpoint file. The skill should address this case.

### Skill gap: validation extensions don't exist in Reflekt
The `create-feature` → `Validator.md` skill references `NotEmptyWithMessage()`, `MaximumLengthWithMessage()`, and `MaxLength` constants from `Blocks.Core`. These don't exist in Reflekt. Standard FluentValidation methods are used instead. Either build the extensions or update the skill for projects without them.

### Skills from other projects may reference MVC attributes
The `EndpointFastEndpoints.md` skill shows `[Authorize(Roles = ...)]` which is an MVC attribute pattern. FastEndpoints has its own attribute set resolved via `global using FastEndpoints`. When adapting skills from MVC-aware projects, be explicit about which attribute namespace to use — the Developer discovered this via CS0104 ambiguity.

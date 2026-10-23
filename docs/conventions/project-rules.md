# Stack Rules

Source-file boundary, conventions and build verification for this repo. Referenced by all agents that plan, write, or review code.

## Source File Definition

Source files are files that contain application code. Agents restricted from writing source code must not create or modify these files:

- `src/**/*.cs` — all C# files of the service projects (`src/Services/Reflekt/`) and the building blocks (`src/BuildingBlocks/`)
- `web/reflekt-web/src/**/*.ts` — TypeScript files
- `web/reflekt-web/src/**/*.vue` — Vue single-file components
- `web/reflekt-web/src/**/*.css` — stylesheets

These are NOT source files (agents may read/write them freely):
- `docs/**` — documentation, specs, plans
- `.claude/**` — agent definitions, rules, skills
- `web/reflekt-web/index.html`, `web/reflekt-web/*.config.*` — frontend build config
- Root config files (`*.slnx`, `*.json`, `*.md`)

## Guardrails, repo structure, reference graph

They live in the root `CLAUDE.md` only: § Guardrails — DO NOT, § Repo Structure and its § Project reference graph. Read them there; this file does not repeat them.

## Conventions

- **Domain events:** the rule and its exception are in the root `CLAUDE.md` § Domain events.
- **Aggregate creation:** static factory when business rules or domain events are involved; `required init` properties when plain data.
- **FastEndpoints:** one endpoint class per feature; its request, response and validator live in the slice's sibling `*Command.cs` / `*Query.cs`.
- **Repositories** wrap `SaveChangesAsync` — endpoints never touch DbContext directly.
- **EF migrations** (from the repository root): `dotnet ef migrations add Name -p src/Services/Reflekt/Reflekt.Persistence -s src/Services/Reflekt/Reflekt.API`

## Plan-to-Code Boundary

- **Plan steps describe operations and acceptance criteria — not method body logic flows.** The developer decides internal code structure and decomposition.
- **When a plan step has >5 sequential sub-operations**, indicate they should be decomposed into private methods — don't describe one monolithic method.

## Build Verification

- `dotnet build` — run after every backend implementation step. Fresh output, not assumed.
- `npm run build` (from `web/reflekt-web/`) — run after frontend changes. Must exit 0 with no TypeScript errors.
- `npm --prefix web/reflekt-web run lint` — ESLint and Prettier, run after frontend changes. Must exit 0.
- Frontend tests are not set up: there is no frontend test runner and no test script.
- **File-lock MSB errors (MSB3026/MSB3027/MSB3492) on Windows are not compilation errors** — they mean the app or dev server is holding output DLLs. Filter with `error CS` to confirm zero actual compiler errors. Don't chase these as build failures.
- Debug artifact grep: `Console.WriteLine` (debugging), `TODO`, `HACK`, `FIXME`, commented-out code.

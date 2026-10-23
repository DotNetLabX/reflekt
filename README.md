# Reflekt

Reflekt is a sprint retrospective app for small teams. A Scrum Master signs in with Google, creates a team and a sprint, and shares a join link; participants join with a display name (or their Google account). The retro runs in fixed phases — Writing, Grouping, Voting, Discussing, Reporting — with live updates for everyone in the room: cards in configurable columns, drag-and-drop grouping, dot voting on groups, a focused discussion flow with action items, evidence links (Jira issues, pull requests, commits) on cards, a per-phase timer, and a report at the end. Open action items from the previous retro come back at the start of the next one.

It is also a teaching repo: the whole app was built by AI agents. See [How this repo was built](#how-this-repo-was-built).

## Stack

- **Backend:** .NET 10, ASP.NET Core, [FastEndpoints](https://fast-endpoints.com/) (one class per endpoint, vertical slices), FluentValidation, EF Core with SQLite, SignalR for real-time updates, Google OAuth (cookie auth) for the Scrum Master and guest tokens for participants
- **Frontend:** Vue 3, TypeScript, Pinia, Tailwind CSS, built with Vite into the API's `wwwroot/` and served by the API as static files
- **Architecture:** DDD aggregates and domain events, vertical slice features, one service (`src/Services/Reflekt`) split into API / Domain / Persistence projects, plus shared building blocks under `src/BuildingBlocks`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+ (with npm)
- A Google Cloud project with an OAuth 2.0 Client ID (needed to sign in as Scrum Master; placeholder values are enough to run as a guest)
- Azure CLI — only if you deploy

## Run locally

### 1. Google OAuth credentials

The API's authentication setup needs non-empty Google client values, even if you only use Reflekt as a guest. Real values are needed to sign in with Google; to run as a guest only, harmless placeholder values (any non-empty text, such as `placeholder`) are enough.

For real values: in [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials, create an OAuth 2.0 Client ID (Web application) and add as authorized redirect URI the origin you browse from, e.g. `http://localhost:5173/signin-google` (Vite dev server) and `http://localhost:4001/signin-google` (API directly).

Store the values (real or placeholder) with .NET user secrets — never in `appsettings*.json`. From the repository root:

```powershell
cd src/Services/Reflekt/Reflekt.API
dotnet user-secrets set "Auth:Google:ClientId" "<YOUR_CLIENT_ID>"
dotnet user-secrets set "Auth:Google:ClientSecret" "<YOUR_CLIENT_SECRET>"
```

### 2. Backend

From the repository root:

```powershell
cd src/Services/Reflekt/Reflekt.API
dotnet run
```

The API listens on `http://localhost:4001`. On startup it creates `data/reflekt.db` and applies the EF Core migrations — there is no database to set up by hand.

**Sample data (`Seed` setting).** In the Development environment you can seed a team, a sprint and a few participants. Set `Seed:Enabled` to `true` and `Seed:SeedUserEmail` to the Google email you will sign in with (so the seeded team is yours), for example with user secrets, run from `src/Services/Reflekt/Reflekt.API`:

```powershell
dotnet user-secrets set "Seed:Enabled" "true"
dotnet user-secrets set "Seed:SeedUserEmail" "you@example.com"
```

### 3. Frontend

In a second terminal, from the repository root:

```powershell
cd web/reflekt-web
npm install
npm run dev
```

Open `http://localhost:5173`. The Vite dev server proxies `/api`, `/auth`, `/signin-google` and the SignalR hubs to the API on port 4001, so run both.

## Tests and build

From the repository root:

```powershell
# Backend: unit and integration tests
dotnet test Reflekt.slnx

# Frontend: type-check and production build (output goes to the API's wwwroot/)
cd web/reflekt-web
npm ci
npm run build
```

## Deploy

`deploy.ps1` builds the frontend, publishes the API, zips it and deploys it to an Azure App Service (Linux). Both parameters are mandatory — the script never targets a default app. From the repository root:

```powershell
.\deploy.ps1 -AppName <APP_NAME> -ResourceGroup <RESOURCE_GROUP>
```

Creating the Azure resources, the app settings (Google credentials, SQLite path) and the OAuth redirect URIs for the deployed site are covered step by step in [docs/deployment-azure.md](docs/deployment-azure.md).

## How this repo was built

Reflekt was built by AI agents (Claude Code), with a human owner deciding what to build and approving each step. The repo keeps the artifacts of every generation of that process on purpose — they are the record of how the app came to be, not documentation of the current code. Three generations, oldest first:

| Where | Generation | What you find |
|-------|------------|---------------|
| `ai-coding/` | First | The original agent setup and per-feature folders (`ai-coding/plans/{Feature}/`) — usually a plan, a review and lessons, with the delivery record as `done.md` in the earlier folders and as `implementation.md` + `summary.md` in the later ones; a few folders hold only part of this set — plus notes on the development flow and evaluations of the setup |
| `docs/features/` + `docs/plans/` | Second | Feature specs written before planning (`docs/features/`) and their implementation plans and delivery records (`docs/plans/`) |
| `docs/specs/` | Current | The current pipeline layout: one folder per feature from `F29-…` onward, plus ad-hoc runs (`adhoc-…`). Each has `delivery/` (plan, implementation record, reviews, lessons, summary); `F31`, `F32` and `F35`–`F40` also have `definition/` (the spec), while `F29`, `F30`, `F33`, `F34` and the ad-hoc run have `delivery/` only |

Around them:

- `docs/product/` — the business specs (`v1.md`, `v2.md`); [`docs/architecture/index.md`](docs/architecture/index.md) — the one-page architecture map, which leads to the as-built reference `architecture.md` (`v1.md` beside it is the original proposal, kept as the decision log); `docs/backlog.md` — the feature sequence and status; `docs/bugs.md` — bugs found in user testing and how they were fixed; `docs/conventions/` — the coding conventions the agents follow.
- `.claude/` — the agent setup exactly as it was used: agent roles (`agents/`), always-on rules (`rules/`), artifact formats (`conventions/`) and pattern skills (`skills/`). `CLAUDE.md` at the root holds the project-wide instructions.
  - The skill set started from a starter template, so it includes skills this app never used (for example gRPC contracts, Redis persistence, MassTransit integration events, MediatR pipeline behaviors, analytics services); their examples point at the template's own paths.
  - The rules mention a `docs/kb/` knowledge base (and a `docs/skill-backlog.md`). Neither was ever created — the mentions are left as they were written.
- `graphify-out/` — a structural knowledge graph of the code (`GRAPH_REPORT.md`) that the agents read before answering architecture questions.

## License

MIT — see [LICENSE](LICENSE).

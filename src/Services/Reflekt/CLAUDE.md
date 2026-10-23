# Reflekt Service

## Service axes

- **Service index:** `1` (host ports derive from the root `CLAUDE.md` § Port Convention table)
- **Database:** SQLite file at `./data/reflekt.db`, connection string key `ConnectionStrings:Reflekt`
- **Auth schemes:** `Cookies` (default; sign-in through `Google`), `Google`, `GuestToken` (custom handler `GuestTokenAuthenticationHandler`)
- **Guest-token transports**, tried in order: header `X-Guest-Token`; query `access_token` on `/hubs/*` paths; `Authorization: Bearer …`
- **Policies:** `SignedInUser` (Cookies), `Guest` (GuestToken), `SignedInUserOrGuest` (either)
- **SignalR:** one hub `SprintHub` at `/hubs/sprint` (GuestToken scheme), groups `sprint:{sprintId}` and `participant:{participantId}`
- **BuildingBlocks used:** all four — `Blocks.Domain` (via `Reflekt.Domain`), `Blocks.Persistence` (via `Reflekt.Persistence`), `Blocks.Core` and `Blocks.Exceptions` (referenced by `Reflekt.API`)

## What is NOT duplicated here

Port numbers, the full tech stack, and guardrails live in root `CLAUDE.md`. The as-built reference is `docs/architecture/architecture.md`.

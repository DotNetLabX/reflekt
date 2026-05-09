# Reflekt Service

## Service axes

- **Service index:** `1` (host port derives from root CLAUDE.md port table → HTTP `4001`)
- **Database:** SQLite file at `./data/reflekt.db`, connection string key `ConnectionStrings:Reflekt`
- **Auth schemes:** `Cookies` (backed by `Google`), `GuestToken` (custom handler, header `X-Guest-Token`)
- **SignalR:** one hub `SprintHub` at `/hubs/sprint`, groups `sprint:{sprintId}`
- **BuildingBlocks used:** `Blocks.Domain`, `Blocks.Persistence`

## What is NOT duplicated here

Port numbers, the full tech stack, and guardrails live in root `CLAUDE.md`.

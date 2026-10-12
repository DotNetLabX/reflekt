# reflekt-web

The Reflekt frontend: Vue 3 + TypeScript + Pinia + Tailwind CSS, built with Vite.

## Develop

```sh
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and proxies `/api`, `/auth`, `/signin-google` and the SignalR hubs (`/hubs`) to the API on `http://localhost:4001` — start the API too (`dotnet run` in `src/Services/Reflekt/Reflekt.API`).

## Build

```sh
npm run build
```

Type-checks with `vue-tsc`, then builds into `../../src/Services/Reflekt/Reflekt.API/wwwroot/`, where the API serves it as static files. That folder is git-ignored. For a clean install from the lockfile, use `npm ci`.

Setup, Google sign-in, tests and deployment are in the [root README](../../README.md).

# Lessons: adhoc-ReadmeRewrite

## Solo Lessons

- **Screenshots of a real-time app need no accounts or manual clicking.** Fill a throwaway SQLite file, start the API against it through `ConnectionStrings__Reflekt`, hand the browser a participant's guest token through `localStorage`, and move the phase by updating the row. A signed-in screen can have its `/auth/me` and list calls answered by the capture script.
- **Timers live in memory and keep running between captures.** An expired timer shows a red `00:00`. Restart the API before the capture that shows the timer.
- **Found, not fixed: `npm run lint` does not exist.** `CLAUDE.md`, `docs/architecture/architecture.md` and `docs/conventions/project-rules.md` tell readers to run `npm --prefix web/reflekt-web run lint`, and `eslint.config.js` and `.prettierrc.json` are tracked, but `web/reflekt-web/package.json` has never had a `lint` script or the ESLint and Prettier packages. The new README does not mention linting.

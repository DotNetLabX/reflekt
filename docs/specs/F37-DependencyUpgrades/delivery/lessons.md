# F37-DependencyUpgrades — Lessons

## Architect Lessons

- In this repo the declared `unit` role runs only `tests/Reflekt.UnitTests`; the integration suite (where `Database.Migrate()` catches model drift) runs only in the `full` role. A plan step that needs "the full suite" must name `verify-run.js --role full`, or the step close silently skips the check that matters. (F37 Q2.)
- `npm audit fix --dry-run` prints the change list only once `node_modules` exists; without it, it prints the plain audit. A change note that cites the dry run must schedule it after an `npm ci`.
- A verification probe must take the exact route the spec names. The spec said "`sqlite_version()` through the app's connection" and "selectors against a saved page"; my first draft opened a separate `SqliteConnection` and invented a live-URL route, and round 1 caught both (F37 plan R1-01, R1-02). Equality checks need a non-degenerate guard: two `Failed`/null results compare equal.
- In a campaign worktree the factory fence refuses a recursive force-delete of a top-level folder (and any shell command whose text contains one); plan scratch cleanup as `rm -r {dir}/{sub}` then `rmdir {dir}`.

## Developer Lessons

- A failing `verify-run.js --role full` record keeps only the head and tail of the output (`output_excerpt`, the middle "… [N chars elided] …"), so an integration test that fails at host start loses its exception type and message — only the stack frames survive. And in this repo `--files` on an integration test file falls back to `roles.unit` ("no installed plugin runs vstest tests"), so a developer seat cannot re-run that one test to get the message. When a full-run red needs its message, say so in the hand-back so the coordinator can capture it from the main session or a solo. (F37 Step 2, run `mvkgn2o8-461c60a6`.)

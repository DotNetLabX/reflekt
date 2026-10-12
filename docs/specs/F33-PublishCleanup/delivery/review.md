# F33 — Publish Cleanup — Review

## Step 2 — Code Review

**Verdict: REQUEST CHANGES**
Primary: Codex — NO-GO, persisted by the architect main session (light lane, no team lead)
**Model:** codex

Full Codex output: `review-codex.md`. Second reader (sonnet): `review-second.md` — passed with 1 MEDIUM and 4 LOW. Run record: complete run `pass` at 2026-10-12T23:00:03Z (backend suite + frontend build, 55 s, tree hash `23d7077e…`).

### [HIGH] Integration tests depend on Google credentials present on the build machine
`tests/Reflekt.IntegrationTests/*` (e.g. `TeamEndpointTests.cs:36`): the test hosts keep the production Google handler without supplying OAuth options; the suite passed only because this machine has the API's user-secrets set. Architect reproduction: with the two Google settings blanked by environment variable, `TeamEndpointTests` fails (400 instead of 200/201). A student's clean clone would see a red suite. Fix: every test factory (authenticated, anonymous, smoke) supplies harmless non-empty OAuth values.

### [MEDIUM] README describes Google configuration as optional
`README.md:22`: authentication start-up needs non-empty client values even for guest-only use. Fix: say real values are needed for Google sign-in and harmless placeholder values are enough to run as a guest.

### [MEDIUM] Absolute machine path in `done-check.md`
`done-check.md:13`. Fixed by the architect before this list (path replaced by a description) — recorded, not sent.

### [MEDIUM] Retract test no longer proves whose vote was removed
`tests/Reflekt.IntegrationTests/VoteEndpointTests.cs:195`: with two participants voting, asserting only the remaining total would also pass if the other participant's vote were removed. Fix: assert both initial votes succeed, the caller's own vote count drops to zero, and the other participant keeps theirs.

### [LOW] README command blocks assume working directories they do not state
`README.md:37` and after. Fix: state the directory each block runs from.

*Status: COMPLETE — Codex, persisted by the architect main session, 2026-10-13*

## Step 2 — Fix list, cycle 1

Merged from Codex (primary), the second reader and the done check (PASS, no items). Light lane: one round, every item at any level; no follow-up review; no done-check re-check.

1. **[HIGH]** Test hosts must not depend on machine credentials — every `WebApplicationFactory` in `tests/Reflekt.IntegrationTests` (authenticated, anonymous, smoke) supplies harmless non-empty `Auth:Google:ClientId` / `ClientSecret`. Prove it: run the integration suite with both settings blanked by environment variable (`Auth__Google__ClientId=` and `Auth__Google__ClientSecret=` on the runner's command) — green. *(Codex)*
2. **[MEDIUM]** `README.md` Google section: real client values are needed for Google sign-in; harmless placeholder values are enough to run as a guest. *(Codex)*
3. **[MEDIUM]** `VoteEndpointTests.cs` retract test: assert both initial votes succeed, the caller's own count becomes zero, the other participant keeps theirs. *(Codex)*
4. **[LOW]** `VoteEndpointTests.cs` `CastVote_SameGroupTwice_Returns409`: assert the first vote returns `Created` before asserting the second is 409. *(second reader)*
5. **[LOW]** `README.md`: state the working directory of each command block; make the generations guide exact (some first-generation plan folders hold `done.md` rather than `implementation.md`; only the newest specs have a `definition/` folder). *(Codex, second reader)*
6. **[LOW]** The sibling app's ticket-key prefix in `.claude/agents/po.md:43` and `docs/conventions/testing.md:48` → the neutral `RET` prefix used in step 4. Run step 4's four greps again plus `git grep -n -I -E 'F[O]K-[0-9]'` — zero. *(second reader)*
7. **[LOW]** `.claude/skills/service-registration/SKILL.md` host-composition example: the Reflekt-named symbols that do not exist in Reflekt become neutral `{App}` placeholders (`Add{App}Module`, `{App}DbContext`, `Map{App}Endpoints`), as plan step 4 said for sites with no Reflekt equivalent. *(second reader)*

Follow-up review: none due (light lane).

**Not on this list (decided by the architect, owner away):** the owner's first name in the product/architecture docs stays (the author is named in `LICENSE` anyway); feature slugs in `known-deviations.md` stay (they do not name the sibling app); whether `docs/conventions/testing.md` should describe Reflekt's own test helpers goes to F34.

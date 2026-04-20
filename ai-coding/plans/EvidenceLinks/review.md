# EvidenceLinks — Code Review

**Reviewer:** Architect
**Date:** 2025-04-20

## Verdict: 4 fixes required (1 already applied, 3 from second review)

Overall: very clean implementation. Every plan step was followed accurately. Domain model, persistence, endpoints, event handlers, background service, frontend store, SignalR, and tests all match the plan specifications exactly. The Q1 deviation (Step 12 file target) was correctly applied.

A second independent review (external agent) caught 3 additional issues the Architect missed — a persistence truncation bug, a missing optimistic reconciliation, and a relative URL resolution issue. These are real bugs that must be fixed before shipping.

---

## Finding 1 (Fix Required): Pending evidence links are not clickable

**File:** `web/reflekt-web/src/components/CardItem.vue`, line 47
**Plan reference:** Step 12 — "Clicking any evidence link opens the URL in a new tab (`target="_blank"`, `rel="noopener noreferrer"`)"
**Spec reference:** Flow 4 step 5 — "Clicking any evidence link preview opens the URL in a new tab"; Acceptance criterion — "Clicking an evidence link preview opens the URL in a new tab"

**Current code:**
```html
:href="link.fetchStatus !== 'Pending' ? link.url : undefined"
:class="link.fetchStatus === 'Pending' ? 'cursor-default' : 'cursor-pointer'"
```

Pending links have no `href` and `cursor-default` — they're not clickable. The spec says **any** evidence link should open in a new tab. The user pasted the URL; they know what it is and should be able to click it while waiting for enrichment.

**Fix:** Always set `:href="link.url"` and always use `cursor-pointer`. The Pending visual treatment (dimmed text, animated dots) is correct and should stay — just make the link functional.

**Status: FIXED**

---

## Finding 2 (Fix Required): Truncate OpenGraph fields before persisting

**File:** `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphBackgroundService.cs`, line 29
**Plan reference:** Step 8 — background service calls `card.UpdateEvidenceLinkMetadata(...)`
**Spec reference:** Entity definition — Title max 500, Description max 1000

A page with og:title > 500 chars or og:description > 1000 chars will cause `SaveChangesAsync` to throw (EF Core max length constraint). The `catch` block logs the error, but the link stays Pending forever — no Failed status set, no `EvidenceLinkFetched` event fires.

**Fix:** Truncate `result.Title` and `result.Description` before passing to `UpdateEvidenceLinkMetadata`. Apply in the background service (not the domain entity — the entity shouldn't know about DB column limits):

```csharp
var title = result.Title?[..Math.Min(result.Title.Length, 500)];
var description = result.Description?[..Math.Min(result.Description.Length, 1000)];
card.UpdateEvidenceLinkMetadata(job.EvidenceLinkId, title, description, result.IconUrl, result.ImageUrl, FetchStatus.Ok);
```

---

## Finding 3 (Fix Required): Replace optimistic evidence link with POST response

**File:** `web/reflekt-web/src/views/RetroRoomView.vue`, lines 388-390
**Plan reference:** Step 12 — optimistic UI for attach
**Spec reference:** Flow 1 — "The new evidence link is returned immediately"

The `attachEvidenceLink` handler pushes an optimistic link (negative ID, Pending) but ignores the API response. It relies entirely on the `CardUpdated` SignalR event to reconcile. If SignalR misses the event (reconnecting), the negative-ID placeholder stays forever: Jira links never show the issue key chip, and the user can't detach the real server-side link.

The existing `addNote` function in the same file already does this correctly — it replaces the optimistic card with the API response.

**Fix:** Replace the optimistic link with the real response after the API call succeeds:

```typescript
try {
    const real = await cardStore.attachEvidenceLink(sprintId, cardId, url)
    const idx = card.evidenceLinks.findIndex((e) => e.id === optimistic.id)
    if (idx !== -1) card.evidenceLinks[idx] = real
} catch {
```

---

## Finding 4 (Fix Required): Resolve relative icon URLs against the evidence URL

**File:** `web/reflekt-web/src/components/CardItem.vue`, lines 222-224 (faviconUrl helper)
**Plan reference:** Step 12 — favicon display
**Spec reference:** Flow 4 — "favicon + title" display

If `link.iconUrl` is a relative path (e.g., `/favicon.ico` extracted by AngleSharp), the browser resolves it against the retro app's origin, not the target site. The favicon will be wrong or broken.

**Fix:** In the `faviconUrl` helper, resolve `link.iconUrl` against `link.url` when it's relative:

```typescript
function faviconUrl(link: EvidenceLink): string | undefined {
  if (link.iconUrl) {
    try {
      return new URL(link.iconUrl, link.url).href
    } catch {
      return link.iconUrl
    }
  }
  try {
    const { origin } = new URL(link.url)
    return `${origin}/favicon.ico`
  } catch {
    return undefined
  }
}
```

---

## Note (No Fix Required): SSRF redirect gap

**File:** `src/Services/Reflekt/Reflekt.API/OpenGraph/OpenGraphFetcher.cs`

HttpClient follows redirects automatically, but the SSRF check only validates the original hostname. An attacker-controlled URL could redirect to a private IP. The spec explicitly accepts this: "Basic hostname validation — sufficient for internal team use" (business rule 9). Downgraded from the external reviewer's P1 to a known limitation for v1. Logged in lessons.md for future hardening (disable `AllowAutoRedirect`, manually follow redirects with per-hop validation).

---

## Finding 5 (Minor — No Fix Required): Dead ternary in test helpers

**File:** `tests/Reflekt.IntegrationTests/EvidenceLinkEndpointTests.cs`, lines 384, 392

```csharp
return asScrumMaster ? _client.SendAsync(req) : _client.SendAsync(req);
```

Both branches are identical — `asScrumMaster` has no effect. The tests work correctly because the SM path passes `token: null` (no guest header) and `_client` has `TestAuthHandler` which authenticates as the SM email. The ternary is just dead code. Not a bug, but noise.

**No fix required** — the tests pass correctly. Developer can clean up at their discretion.

---

## Verified Items

| Area | Status | Notes |
|------|--------|-------|
| EvidenceLink entity (11 properties, factory, internal method) | Correct | Matches plan §3.2 exactly |
| Enums (EvidenceLinkSource, FetchStatus) | Correct | |
| Domain events (3 records) | Correct | Signatures match plan §3.4 |
| Card aggregate (backing field, 3 methods, guards) | Correct | Matches plan §3.3 |
| EvidenceLinkConfiguration (table, columns, FK, index) | Correct | Matches plan §4.2 |
| CardConfiguration (HasMany, cascade, backing field access) | Correct | |
| CardRepository (GetByIdAsync override, Include on both queries) | Correct | |
| ReflektDbContext (DbSet added) | Correct | |
| EvidenceLinkSourceDetector (GitHub/Jira/Generic rules, regex) | Correct | Matches plan §5 |
| AttachEvidenceLinkEndpoint (route, auth, handler flow, 201) | Correct | All 9 handler steps present |
| DetachEvidenceLinkEndpoint (route, auth, handler flow, 204) | Correct | All 9 handler steps present |
| Validator (NotEmpty, MaxLength 2048, HTTP/HTTPS scheme) | Correct | |
| CardContracts (EvidenceLinkResponse, CardResponse update, mappings) | Correct | Enums serialized as strings |
| EvidenceLinkAttachedEventHandler (CardUpdated + FetchJob if Pending) | Correct | |
| EvidenceLinkFetchedEventHandler (EvidenceLinkFetched event) | Correct | |
| EvidenceLinkDetachedEventHandler (CardUpdated) | Correct | |
| OpenGraph FetchJob (CardId field added) | Correct | |
| OpenGraph BackgroundService (scoped DB update, try/catch) | See Finding 2 | Missing field truncation |
| Seed data (GitHub + Generic links, idempotent) | Correct | |
| appsettings (JiraDomain config) | Correct | |
| card.ts (EvidenceLink interface, Card.evidenceLinks, 2 actions) | Correct | |
| useSprintHub.ts (EvidenceLinkFetched listener) | Correct | |
| CardItem.vue (display, attach UI, detach UI, favicon helper) | See Findings 1, 4 | Pending clickable (fixed); relative icon URLs |
| RetroRoomView.vue (event wiring on all 3 columns, optimistic UI) | See Finding 3 | Missing optimistic reconciliation |
| Unit tests (12 aggregate + 5 detector = 17) | Correct | All plan cases covered |
| Integration tests (11 attach + 5 detach + 3 card = 19) | Correct | All plan cases covered |

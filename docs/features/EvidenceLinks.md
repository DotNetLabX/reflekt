# Evidence Links

**Traces to:** `docs/specs/v1.md` §4 (EvidenceLink entity), §6 (evidence links), §7.1 (SignalR: EvidenceLinkFetched), §8 (action items can have evidence links)
**Dependencies:** Auth, Teams, Sprints, Participants, Cards
**Status:** Ready

---

## Purpose

Evidence links are Reflekt's differentiator. Cards can carry previews of GitHub PRs, Jira issues, or any URL — giving the team concrete evidence during discussion instead of "I think there was a PR for that." Links are enriched asynchronously via server-side metadata fetching, so the UI is never blocked waiting for a slow external site.

## Entities

### EvidenceLink (child entity of Card)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| CardId | int | Required, references Card (parent) |
| Url | string | Required, max 2048 chars, must be HTTP or HTTPS |
| Source | enum | GitHub, Jira, Generic — auto-detected from URL |
| Title | string? | Nullable, max 500 chars, populated by metadata fetch |
| Description | string? | Nullable, max 1000 chars, populated by metadata fetch |
| IconUrl | string? | Nullable, populated by metadata fetch (favicon or og:image icon) |
| ImageUrl | string? | Nullable, populated by metadata fetch (og:image) |
| ExternalKey | string? | Nullable, max 50 chars — e.g. "PROJ-123" for Jira, extracted from URL |
| FetchedAt | DateTime? | Nullable, UTC, stamped when metadata fetch completes (success or failure) |
| FetchStatus | enum | Pending, Ok, Failed |

An EvidenceLink belongs to one Card. A Card can have many EvidenceLinks. The lifecycle of an EvidenceLink is tied to its parent Card — deleting a card deletes all its evidence links.

EvidenceLinks are created with FetchStatus = Pending. The source is auto-detected from the URL at creation time:
- **GitHub** — URL host is `github.com`
- **Jira** — URL host matches a configured Jira domain, OR URL path matches the `/browse/{KEY}` pattern
- **Generic** — anything else

For Jira URLs, the issue key (e.g. `PROJ-123`) is extracted from the URL and stored in ExternalKey at creation time — no API call needed.

**Domain events (raised by the parent Card aggregate):**
- EvidenceLinkAttached — raised when an evidence link is added to a card
- EvidenceLinkFetched — raised when the background metadata fetch completes (success or failure)

## User Flows

```
Flow 1: Attach an evidence link
1. Participant pastes or types a URL while working on a card
2. Frontend detects the URL and sends an attach request
3. Server verifies the caller can modify this card (card author or Scrum Master) and the sprint is not Closed
4. Server auto-detects the source (GitHub, Jira, Generic) from the URL
5. For Jira URLs, the issue key is extracted from the URL path
6. EvidenceLink is created with FetchStatus = Pending
7. For GitHub and Generic sources, a background metadata fetch is queued
8. For Jira sources, no fetch is queued — the issue key chip is the display
9. The new evidence link is returned immediately (Pending status)
10. All connected clients receive the evidence link data via the card
```

```
Flow 2: Background metadata fetch completes
1. Background service picks up the fetch job
2. Server-side HTTP request to the URL with a 5-second timeout
3. HTML response is parsed for OpenGraph meta tags (og:title, og:description, og:image)
4. On success: EvidenceLink is updated with metadata, FetchStatus = Ok, FetchedAt stamped
5. On failure: FetchStatus = Failed, FetchedAt stamped, no metadata stored
6. All connected clients in the sprint receive an EvidenceLinkFetched notification with the full evidence link data
```

```
Flow 3: Detach an evidence link
1. Card author or Scrum Master removes an evidence link from a card
2. Server verifies ownership (card author or SM) and sprint is not Closed
3. EvidenceLink is removed from the card
4. All connected clients receive a notification that the link was removed
```

```
Flow 4: Evidence links returned with cards
1. When cards are listed or created, evidence links are embedded in the card response
2. Evidence links with FetchStatus = Ok show enriched inline preview: favicon + title + (for Jira) issue key chip
3. Evidence links with FetchStatus = Pending or Failed show the bare URL with a domain favicon (constructed from the URL's domain, e.g. https://github.com/favicon.ico)
4. For Jira links, the issue key chip is always shown regardless of fetch status
5. Clicking any evidence link preview opens the URL in a new tab
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/cards/{cardId}/evidence-links` | Guest token or SM cookie | `{ url }` | Full evidence link object | 201, 400, 401, 403, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/cards/{cardId}/evidence-links/{evidenceLinkId}` | Guest token or SM cookie | — | — | 204, 401, 403, 404, 409 |

Evidence links are also embedded in card responses from the existing card endpoints (GET, POST, PUT on cards).

Error responses:
- **AttachEvidenceLink with empty/missing URL:** 400
- **AttachEvidenceLink with URL exceeding 2048 chars:** 400
- **AttachEvidenceLink with non-HTTP(S) URL scheme:** 400
- **AttachEvidenceLink without valid auth:** 401
- **AttachEvidenceLink by someone other than the card author or SM:** 403
- **AttachEvidenceLink targeting a nonexistent sprint or card:** 404
- **AttachEvidenceLink targeting a Closed sprint:** 409
- **DetachEvidenceLink by someone other than the card author or SM:** 403
- **DetachEvidenceLink targeting a nonexistent evidence link:** 404
- **DetachEvidenceLink targeting a Closed sprint:** 409

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| EvidenceLinkFetched | Full evidence link object with metadata | Background fetch completes (success or failure) |

Note: attach and detach are reflected through CardUpdated (the card's evidence links collection changes). EvidenceLinkFetched is a separate event because it fires asynchronously after the original request has already returned.

## Business Rules

1. Evidence links can be attached to any card type — both notes and action items.
2. Attach/detach permission follows card ownership: card author or Scrum Master. No other participant can modify another's card's evidence links.
3. Evidence links cannot be attached or detached in a Closed sprint. Mutation endpoints return 409.
4. URL must be HTTP or HTTPS. Other schemes (ftp, file, javascript, data) are rejected with 400.
5. Source is auto-detected from the URL at creation time. The caller does not specify the source.
6. For Jira URLs, the issue key is extracted from the URL path using the `/browse/{KEY}` pattern (e.g. `PROJ-123`). No Jira API call is made.
7. Metadata fetching is async and non-blocking. The endpoint returns immediately with FetchStatus = Pending.
8. Metadata fetch has a 5-second timeout. On timeout or any failure, FetchStatus = Failed. No automatic retry.
9. SSRF protection: the fetch service rejects requests to localhost, private IP ranges (10.x, 172.16.x, 192.168.x), and link-local addresses. Basic hostname validation — sufficient for internal team use.
10. For Jira-detected URLs, no metadata fetch is queued — the issue key chip is the primary display. FetchStatus is set to Ok immediately with ExternalKey populated.
11. Deleting a card cascade-deletes all its evidence links.
12. Duplicate URLs on the same card are allowed — no uniqueness constraint.

## Acceptance Criteria

- [ ] `POST /api/sprints/{sprintId}/cards/{cardId}/evidence-links` with a valid GitHub URL creates an evidence link with Source = GitHub and FetchStatus = Pending, returns 201
- [ ] `POST` with a Jira-pattern URL creates an evidence link with Source = Jira, ExternalKey populated, and FetchStatus = Ok, returns 201
- [ ] `POST` with a generic URL creates an evidence link with Source = Generic and FetchStatus = Pending, returns 201
- [ ] `POST` with empty or missing URL returns 400
- [ ] `POST` with URL exceeding 2048 chars returns 400
- [ ] `POST` with a non-HTTP(S) scheme returns 400
- [ ] `POST` without valid auth returns 401
- [ ] `POST` by someone other than the card author or SM returns 403
- [ ] `POST` targeting a nonexistent card returns 404
- [ ] `POST` targeting a Closed sprint returns 409
- [ ] Background fetch for a GitHub URL populates Title, Description, IconUrl, ImageUrl from OpenGraph tags, sets FetchStatus = Ok
- [ ] Background fetch for a generic URL populates available OpenGraph metadata, sets FetchStatus = Ok
- [ ] Background fetch timeout (>5s) sets FetchStatus = Failed, no metadata stored
- [ ] Background fetch to a private IP range is rejected (SSRF protection)
- [ ] Background fetch to localhost is rejected
- [ ] EvidenceLinkFetched SignalR event fires to sprint group when a background fetch completes
- [ ] `DELETE /api/sprints/{sprintId}/cards/{cardId}/evidence-links/{id}` by the card author returns 204
- [ ] `DELETE` by the Scrum Master returns 204
- [ ] `DELETE` by a participant who is neither author nor SM returns 403
- [ ] `DELETE` targeting a Closed sprint returns 409
- [ ] `DELETE` targeting a nonexistent evidence link returns 404
- [ ] Card GET/list responses include embedded evidence links
- [ ] Deleting a card also deletes all its evidence links
- [ ] Domain events raised: EvidenceLinkAttached on attach, EvidenceLinkFetched on fetch completion
- [ ] No Jira API calls are made — only URL pattern matching and key extraction
- [ ] Successful evidence links display inline: favicon + title + (for Jira) issue key chip
- [ ] Failed/pending evidence links display bare URL with domain favicon
- [ ] Clicking an evidence link preview opens the URL in a new tab
- [ ] Build passes with zero errors

## Out of Scope

- **Jira REST API integration** — v1 extracts the issue key from the URL only. Rich Jira metadata requires OAuth setup deferred to v2.
- **GitHub REST API integration** — OpenGraph scrape only. No authenticated GitHub API calls.
- **Card mode display (full preview box)** — v1 is inline only: favicon + title + (Jira) issue key chip. Full preview with description and image is a v2 nice-to-have.
- **Evidence link editing** — links are attached or detached, not modified. To change a URL, detach and re-attach.
- **Automatic retry on fetch failure** — failed fetches stay failed. Manual re-attach if needed.
- **Batch URL detection from card text** — the frontend sends one URL at a time via the attach endpoint. No server-side parsing of card text for URLs.
- **OpenGraph library** — server-side fetch uses HttpClient + AngleSharp for HTML parsing directly.

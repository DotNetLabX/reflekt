# Cards

**Traces to:** `docs/specs/v1.md` §3.3 (ownership semantics), §4 (Card entity), §5.1 (columns), §5.4 (editing & deletion), §5.5 (ordering), §7.1 (SignalR events), §8 (action items)
**Dependencies:** Auth, Teams, Sprints, Participants
**Status:** Ready

---

## Purpose

Cards are the core content of a retro. Participants write observations in the Went Well and Didn't Go Well columns and capture commitments as action items in the Action Items column. Every downstream feature — voting, evidence links, reports — attaches to cards.

## Entities

### Card (aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| SprintId | int | Required, references Sprint |
| ParticipantId | int | Required, references Participant (author) |
| ColumnType | enum | WentWell, DidntGoWell, ActionItems |
| CardType | enum | Note, ActionItem |
| Text | string | Required, max 2000 chars |
| AssigneeName | string? | Nullable; required when CardType is ActionItem, max 50 chars |
| CreatedAt | DateTime | UTC, stamped at creation |
| UpdatedAt | DateTime | UTC, stamped at creation and on every update |

A Card belongs to one Sprint and one Participant (the author). A Sprint can have many Cards.

Cards are created through a guarded factory that enforces required fields, validates the column/type combination, stamps timestamps, and raises a domain event.

**Column/type constraint:**
- Notes (CardType = Note) belong in WentWell or DidntGoWell only.
- Action items (CardType = ActionItem) belong in ActionItems only.

AssigneeName is a string copy of a participant's display name at assignment time — not a foreign key. It captures the commitment even if the assignee doesn't rejoin future sprints. The frontend populates it from a dropdown of current sprint participants; the backend does not validate it against the participant list.

**Domain events:**
- CardCreated — raised when a card is created
- CardUpdated — raised when a card's text or assignee is modified
- CardDeleted — raised when a card is deleted

## User Flows

```
Flow 1: Create a note
1. Participant writes text in the Went Well or Didn't Go Well column
2. Server verifies the participant belongs to this sprint and the sprint is not Closed
3. Card is created with CardType = Note in the chosen column
4. All connected clients in the sprint receive a real-time CardCreated notification
5. Returns the new card
```

```
Flow 2: Create an action item
1. Participant writes text in the Action Items column and selects an assignee from the participant dropdown
2. Server verifies the participant belongs to this sprint and the sprint is not Closed
3. Card is created with CardType = ActionItem and the selected assignee's display name
4. All connected clients in the sprint receive a real-time CardCreated notification
5. Returns the new card
```

```
Flow 3: Edit a card
1. Card author or Scrum Master modifies the card text (or assignee for action items)
2. Server verifies ownership (author or SM) and sprint is not Closed
3. Card is updated, UpdatedAt is stamped
4. All connected clients receive a real-time CardUpdated notification
5. Returns the updated card
```

```
Flow 4: Delete a card
1. Card author or Scrum Master deletes a card
2. Server verifies ownership (author or SM) and sprint is not Closed
3. Card is removed
4. All connected clients receive a real-time CardDeleted notification
```

```
Flow 5: List cards in a sprint
1. Authenticated participant or SM requests all cards for a sprint
2. Server returns all cards in the sprint ordered by creation time (newest first)
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/cards` | Guest token | `{ text, columnType, cardType, assigneeName? }` | Full card object | 201, 400, 401, 404, 409 |
| PUT | `/api/sprints/{sprintId}/cards/{cardId}` | Guest token or SM cookie | `{ text, assigneeName? }` | Full card object | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/cards/{cardId}` | Guest token or SM cookie | — | — | 204, 401, 403, 404, 409 |
| GET | `/api/sprints/{sprintId}/cards` | Guest token or SM cookie | — | `[{ id, sprintId, participantId, columnType, cardType, text, assigneeName, createdAt, updatedAt }]` | 200, 401, 404 |

**Route rationale:** Card endpoints use flat `/api/sprints/{sprintId}/...` routes like Participants — participants interact via room links and only know the sprint ID.

Error responses:
- **CreateCard with empty/missing text:** 400
- **CreateCard with text exceeding 2000 chars:** 400
- **CreateCard with CardType = ActionItem but missing assigneeName:** 400
- **CreateCard with CardType = ActionItem but ColumnType is not ActionItems:** 400
- **CreateCard with CardType = Note but ColumnType is ActionItems:** 400
- **CreateCard without a valid guest token:** 401
- **CreateCard where participant does not belong to this sprint:** 401
- **CreateCard targeting a nonexistent sprint:** 404
- **CreateCard targeting a Closed sprint:** 409
- **EditCard by someone other than the author or SM:** 403
- **EditCard on a Closed sprint:** 409
- **EditCard on a nonexistent card:** 404
- **EditCard with empty text:** 400
- **EditCard with text exceeding 2000 chars:** 400
- **DeleteCard by someone other than the author or SM:** 403
- **DeleteCard on a Closed sprint:** 409
- **DeleteCard on a nonexistent card:** 404
- **ListCards without valid auth (guest token or SM cookie):** 401
- **ListCards targeting a nonexistent sprint:** 404

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| CardCreated | Full card object | Participant creates a card |
| CardUpdated | Full card object | Card text or assignee is modified |
| CardDeleted | Card ID | Card is deleted |

## Business Rules

1. Only authenticated participants in the sprint can create cards. The participant must belong to the target sprint.
2. Cards cannot be created, edited, or deleted in a Closed sprint. All mutation endpoints return 409.
3. Notes can only be placed in the WentWell or DidntGoWell columns. Action items can only be placed in the ActionItems column.
4. Action items require a non-empty AssigneeName (max 50 chars). Notes must not have an AssigneeName.
5. AssigneeName is a string copy — not validated against the participant list. The frontend constrains selection via a dropdown; the backend stores whatever string is provided.
6. Edit permission: card author OR Scrum Master. No other participant can edit another's card.
7. Delete permission: card author OR Scrum Master. No other participant can delete another's card.
8. Concurrent edit conflict resolution: last write wins. No locking, no merge UI.
9. If a card is deleted while someone is editing it, their save fails with 404 (card not found).
10. Phases are soft — participants can create and edit cards in any phase. The current phase is informational context, not enforcement.
11. Card text is required, max 2000 characters.
12. A card's column and type cannot be changed after creation. Editing only updates text and (for action items) assignee.
13. The API returns cards ordered by creation time (newest first). Vote-based re-ordering is a display concern owned by the Voting feature.

## Acceptance Criteria

- [ ] `POST /api/sprints/{sprintId}/cards` with valid note data creates a card, returns 201
- [ ] `POST /api/sprints/{sprintId}/cards` with valid action item data (including assigneeName) creates a card, returns 201
- [ ] CreateCard with empty/missing text returns 400
- [ ] CreateCard with text exceeding 2000 characters returns 400
- [ ] CreateCard with ActionItem type but missing assigneeName returns 400
- [ ] CreateCard with ActionItem type but ColumnType not ActionItems returns 400
- [ ] CreateCard with Note type but ColumnType ActionItems returns 400
- [ ] CreateCard without a valid guest token returns 401
- [ ] CreateCard where participant does not belong to this sprint returns 401
- [ ] CreateCard targeting a nonexistent sprint returns 404
- [ ] CreateCard targeting a Closed sprint returns 409
- [ ] `PUT /api/sprints/{sprintId}/cards/{cardId}` by the card author updates text, returns 200
- [ ] EditCard by the Scrum Master (not the author) succeeds, returns 200
- [ ] EditCard by a participant who is neither author nor SM returns 403
- [ ] EditCard on a Closed sprint returns 409
- [ ] EditCard on a nonexistent card returns 404
- [ ] EditCard stamps a new UpdatedAt value
- [ ] `DELETE /api/sprints/{sprintId}/cards/{cardId}` by the card author returns 204
- [ ] DeleteCard by the Scrum Master succeeds, returns 204
- [ ] DeleteCard by a participant who is neither author nor SM returns 403
- [ ] DeleteCard on a Closed sprint returns 409
- [ ] DeleteCard on a nonexistent card returns 404
- [ ] `GET /api/sprints/{sprintId}/cards` returns all cards in the sprint ordered by creation time descending
- [ ] ListCards requires valid auth (guest token or SM cookie), returns 401 otherwise
- [ ] ListCards targeting a nonexistent sprint returns 404
- [ ] CardCreated SignalR event fires to sprint group when a card is created
- [ ] CardUpdated SignalR event fires to sprint group when a card is edited
- [ ] CardDeleted SignalR event fires to sprint group when a card is deleted
- [ ] Domain events raised: CardCreated on create, CardUpdated on edit, CardDeleted on delete
- [ ] Build passes with zero errors

## Out of Scope

- **Voting on cards** — owned by the Voting feature. Cards defines the entity; Voting adds vote counts and the "no voting on action items" constraint.
- **Evidence links on cards** — owned by the Evidence Links feature. Cards defines the entity; Evidence Links adds URL previews.
- **Card drag-and-drop reordering** — cards have no explicit sort order field. Ordering is by creation time (API) or vote count (frontend display).
- **Moving cards between columns** — column is set at creation and cannot be changed. Delete and recreate if needed.
- **Converting notes to action items (or vice versa)** — CardType is set at creation and cannot be changed.
- **Card grouping or clustering** — no mechanism to merge or group related cards in v1.
- **Draft text sync while typing** — only push on save, per spec §7.2.
- **Card author attribution in UI** — cards are anonymous per spec. The author's ParticipantId is stored for ownership enforcement but not displayed to other participants.

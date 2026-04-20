# Voting

**Traces to:** `docs/specs/v1.md` §5.3 (voting), §4 (Vote entity), §7.1 (SignalR events: VoteCast, VoteRetracted), §3.3 (ownership semantics)
**Dependencies:** Auth, Teams, Sprints, Participants, Cards
**Status:** Ready

---

## Purpose

Voting surfaces what matters most to the team. Participants spend a limited budget of dot votes across note cards to signal which topics deserve discussion time. Vote counts drive card ordering during the Voting and Discussing phases and determine which notes appear in the sprint report.

## Entities

### Vote (aggregate root)

| Field | Type | Constraints |
|-------|------|-------------|
| Id | int | Auto-increment primary key |
| CardId | int | Required, references Card |
| ParticipantId | int | Required, references Participant (voter) |
| CreatedAt | DateTime | UTC, stamped at creation |

A Vote belongs to one Card and one Participant. A Card can have many Votes. A Participant can have many Votes across different cards within the same sprint.

Votes are created through a guarded factory that validates the card and participant references and stamps CreatedAt.

**Uniqueness:** There is no unique constraint on (CardId, ParticipantId) — a participant may place multiple votes on the same card.

**Domain events:**
- VoteCast — raised when a vote is placed
- VoteRetracted — raised when a vote is removed

## User Flows

```
Flow 1: Cast a vote
1. Participant clicks the vote button on a note card
2. Server verifies the participant belongs to this sprint
3. Server verifies the target card is a Note (not an ActionItem)
4. Server verifies the sprint is not Closed
5. Server verifies the participant has remaining vote budget (total votes cast in this sprint < VotesPerParticipant)
6. Vote is created on the card
7. All connected clients in the sprint receive a real-time VoteCast notification with the card's new vote count
8. Returns the updated vote count for the card
```

```
Flow 2: Retract a vote
1. Participant clicks to remove one of their votes from a card
2. Server verifies the participant has at least one vote on this card
3. Server removes one of the participant's votes from the card
4. All connected clients in the sprint receive a real-time VoteRetracted notification with the card's new vote count
5. Returns the updated vote count for the card
```

```
Flow 3: View vote counts
1. Any authenticated participant or Scrum Master requests sprint cards
2. Each card in the response includes its total vote count
3. Vote counts are anonymous — no indication of which participant voted
```

## API Surface

| Method | Route | Auth | Request body | Response | Status codes |
|--------|-------|------|-------------|----------|-------------|
| POST | `/api/sprints/{sprintId}/cards/{cardId}/votes` | Guest token | — | `{ cardId, voteCount, myVoteCount }` | 201, 400, 401, 404, 409 |
| DELETE | `/api/sprints/{sprintId}/cards/{cardId}/votes` | Guest token | — | `{ cardId, voteCount, myVoteCount }` | 200, 401, 404, 409 |
| GET | `/api/sprints/{sprintId}/votes/mine` | Guest token | — | `[{ cardId, count }]` | 200, 401, 404 |

**Response fields:**
- `voteCount` — total votes on the card (from all participants)
- `myVoteCount` — how many of the caller's votes are on this card (enables the retract UI)

**Route rationale:** Vote endpoints are nested under sprints (not teams) because participants interact via the sprint room. The card ID in the route identifies the target; no request body needed for cast/retract since the voter is resolved from the guest token.

Error responses:
- **Cast vote on a nonexistent card:** 404
- **Cast vote on a card not in this sprint:** 404
- **Cast vote on an ActionItem card:** 400 — only Notes are voteable
- **Cast vote when budget exhausted:** 409 — participant has used all their votes
- **Cast vote on a Closed sprint:** 409
- **Cast vote without valid guest token:** 401
- **Retract vote when participant has no votes on this card:** 409
- **Retract vote without valid guest token:** 401
- **Get my votes on a nonexistent sprint:** 404
- **Get my votes without valid guest token:** 401

## SignalR Events

| Event | Payload | Trigger |
|-------|---------|---------|
| VoteCast | `{ cardId, newCount }` | A participant places a vote |
| VoteRetracted | `{ cardId, newCount }` | A participant retracts a vote |

Payload includes only the card ID and new total count — never the voter's identity. Voting is anonymous. Field name `newCount` matches source spec §7.1.

## Business Rules

1. Each participant gets a vote budget equal to the sprint's VotesPerParticipant value (default 3, set at sprint creation). Budget is per-sprint, not per-card.
2. A participant may distribute votes freely — all on one card, spread across cards, or abstain entirely.
3. Multiple votes from the same participant on the same card are allowed (no unique constraint on the pair).
4. Only cards with CardType = Note can be voted on. Votes on ActionItem cards are rejected.
5. Voting is anonymous — card vote counts are visible to everyone, but who voted is never exposed via API or SignalR.
6. Participants can retract their own votes at any time (as long as the sprint is not Closed). Retracting returns one vote to the budget.
7. The Scrum Master participates in voting as a regular participant — SM auth does not grant extra vote manipulation powers.
8. Closed sprints reject all vote operations (cast and retract).
9. Vote count on a card is the total number of Vote rows referencing that card — no separate counter field.
10. The "my votes" endpoint returns the caller's vote distribution so the frontend can render which cards they've voted on and enable retraction UI.

## Acceptance Criteria

- [ ] `POST /api/sprints/{sprintId}/cards/{cardId}/votes` with valid guest token creates a vote, returns 201 with updated counts
- [ ] Cast vote returns the total vote count for the card and the caller's vote count on that card
- [ ] Cast vote when budget is exhausted returns 409
- [ ] Cast vote on an ActionItem card returns 400
- [ ] Cast vote on a Closed sprint returns 409
- [ ] Cast vote on a nonexistent or wrong-sprint card returns 404
- [ ] Cast vote without valid guest token returns 401
- [ ] Multiple votes on the same card from the same participant are accepted (budget permitting)
- [ ] `DELETE /api/sprints/{sprintId}/cards/{cardId}/votes` removes one of the caller's votes, returns 200 with updated counts
- [ ] Retract when participant has no votes on the card returns 409
- [ ] Retract on a Closed sprint returns 409
- [ ] `GET /api/sprints/{sprintId}/votes/mine` returns the caller's vote distribution across cards
- [ ] VoteCast SignalR event fires to sprint group with card ID and new total count
- [ ] VoteRetracted SignalR event fires to sprint group with card ID and new total count
- [ ] SignalR payloads never include voter identity
- [ ] Vote budget is correctly calculated as: VotesPerParticipant minus total votes cast by participant in the sprint
- [ ] Domain events raised: VoteCast on cast, VoteRetracted on retract
- [ ] Build passes with zero errors

## Out of Scope

- **Vote history or audit trail** — no endpoint to see past vote/retract activity. Only current state matters.
- **Vote count caching** — computed from rows on every request. Acceptable for v1 retro scale (~50 cards, ~5 participants).
- **Weighted votes or priority ordering** — all votes are equal. No "super vote" or ranked-choice.
- **Vote visibility modes (show/hide during voting)** — counts are always visible in real-time. No "reveal" moment.
- **Changing VotesPerParticipant mid-sprint** — budget is set at sprint creation and immutable.
- **SM vote override** — SM cannot add or remove other participants' votes.

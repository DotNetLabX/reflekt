# F33 — Questions

## Q1 — Vote tests vs the one-vote-per-group check (step 1)

**From:** developer (dev-s1) · **To:** architect · **Status:** Answered · **Raised:** 2026-10-13

**Question:** Four Vote integration tests (`CastVote_MultipleTimes_SameGroup_Succeeds`, `RetractVote_Returns200`, `GetMyVotes_ReturnsDistribution`, `ListCards_IncludesVoteCount`) cast two votes on one group; `CastVoteEndpoint` rejects the second with 409 "Already voted on this group" (added in `f5b5229`). `docs/product/v1.md:242,558` and `docs/features/Voting.md` allow several votes per group. `CastVote_BudgetExhausted_Returns409` and `RetractVote_ReturnsBudget` pass only because of the duplicate check. Fix the app, or update the tests?

**Answer (architect, 2026-10-13):** The app is right; the tests and the product spec are stale. Evidence: `docs/bugs.md` BUG-007 (source: user testing, 2026-04-22) lists "users could vote multiple times on the same group — no per-group uniqueness check" as a defect, and its fix is exactly this 409 check, shipped in `f5b5229`. That is the owner's later decision, superseding the original v1 wording.
- Update the four tests to the current rule: spread multiple votes across **different** groups (add groups to the fixture as needed); turn `CastVote_MultipleTimes_SameGroup_Succeeds` into the assertion that a second vote on the same group returns 409 (renaming the method to say so is fine). Test count stays the same.
- Make the two accidental passes real: `CastVote_BudgetExhausted_Returns409` and `RetractVote_ReturnsBudget` must exhaust the budget with votes on distinct groups, so their 409 comes from the budget, not the duplicate check.
- Update `docs/product/v1.md` lines 242 and 558 to the one-vote-per-group rule, pointing at BUG-007 in `docs/bugs.md`. Leave `docs/features/Voting.md` as it is (evidence).
- No change under `src/`.

**Confidence:** high — the bug log records the rule as a deliberate fix from user testing, and the code matches it exactly.
**Owner-away note:** answered by the architect from the repo's own record; listed for the owner at the end of the run.

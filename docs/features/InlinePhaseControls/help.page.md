# Inline Phase Controls — Guide

Guide page content for UI elements. Each section explains interpretation and recommended actions.

---

## Next Phase Button

The "Next: {phase}" button in the top-right corner of the retro room advances the retro to the next phase. Only the Scrum Master sees this button. Clicking it opens a confirmation dialog — you'll always get a chance to cancel before the transition happens. Phase advances are permanent and cannot be undone, so make sure your team is ready before proceeding. During Writing and Voting, check the readiness badge to gauge whether participants need more time.

## Close Sprint Button

During the Reporting phase, the advance button is replaced by "Close Sprint." Closing a sprint transitions it from Active to Closed — the board becomes fully read-only for everyone. Participants stay on the board and can still read the report or copy it as markdown. Use this when the team has finished reviewing the report and captured all action items. Once closed, no further edits are possible.

## Phase Stepper

The horizontal stepper at the top of the retro room shows all five phases: Writing, Grouping, Voting, Discussing, and Reporting. Completed phases show a checkmark, the current phase is highlighted, and upcoming phases are dimmed. This is a display element — it cannot be clicked to jump between phases. Everyone in the retro sees the same stepper, updated in real-time when the Scrum Master advances.

## I'm Done Button

During Writing and Voting phases, every participant sees an "I'm done" button. Click it to let the team (and especially the Scrum Master) know you've finished writing cards or casting votes. If you change your mind — maybe you thought of one more thing — click the button again to toggle back to not-ready. Your readiness state resets automatically when the phase advances, so you don't need to manage it across phases.

## Readiness Badge

The readiness badge (e.g., "4/6 done") appears in the top bar during Writing and Voting phases. It shows how many participants have clicked "I'm done" out of the total. The Scrum Master can hover over the badge to see exactly who is done and who is still working. This is purely informational — it never blocks the Scrum Master from advancing. It helps the SM make a judgment call: wait for stragglers or move on to keep the retro on schedule. The badge is hidden if there's only one participant in the sprint.

## Voting Warning (Advance Dialog)

When the Scrum Master advances from Voting to Discussing, the confirmation dialog shows how many participants haven't cast any votes yet. This count is provided by the server since individual voting is anonymous — the frontend cannot see who has voted. This is a soft warning, not a hard block — the SM can always proceed. The intent is to prevent accidentally cutting off the voting phase while participants are still thinking. If the count is high, consider giving the team another minute. If participants are AFK or disengaged, there's no need to wait.

## Discussion Gate (Advance Dialog)

When the Scrum Master advances from Discussing to Reporting, at least 2 groups must have been marked as discussed. If the minimum isn't met, the system blocks the advance and tells you how many groups have been discussed so far. This gate exists to prevent skipping the discussion phase entirely. To discuss a group, use the "Focus" control on a group during Discussing — when focus moves away, the group is automatically marked as discussed. You can also toggle the discussed state manually.

## Sprint Closed Notification

When the Scrum Master closes the sprint, all participants receive a notification: "This retro has been closed by the Scrum Master." The board transitions to a fully read-only state — no new cards, no edits, no votes. You can still view the report, toggle between report and board views, and copy the report as markdown. If you need to reference the retro later, the sprint remains accessible from the team's sprint list on the sprint manager page.

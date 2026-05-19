# Timer per Phase — Guide

Guide page content for UI elements. Each section explains interpretation and recommended actions.

---

## Timer Display

The timer countdown appears in the top bar during Writing, Voting, and Discussing phases. When active, all participants see the same countdown in real-time — there's no per-person timer. The Scrum Master starts the timer when the team is ready to begin. If no timer is running, participants see nothing in the timer area. The timer is a pacing tool, not a hard limit — when it expires, the board stays fully interactive and you can keep working until the Scrum Master advances the phase.

## Start Timer

The start control is visible only to the Scrum Master and shows the phase's default duration (e.g., "5:00" for Writing). Click it to begin the countdown. Starting a timer is always optional — if the team works better without time pressure, skip it. The default duration was set when the sprint was created, but the SM can extend time or reset during the retro. Timers are not available during Grouping (SM-driven, no need to timebox) or Reporting (read-only, no actions to rush).

## Pause / Resume

Pausing the timer freezes the countdown at its current value. All participants see that the timer is paused. Use this when the team needs an unplanned break or side discussion — it's better than letting the clock run down on something off-topic. Click the same button again to resume from where you left off. Pausing does not reset the timer or change the remaining time.

## Reset Timer

Resetting returns the timer to the phase's configured default duration (e.g., back to 5:00 for a Writing phase with a 5-minute default). After reset, the timer is paused — you need to click start again to begin the countdown. Use reset when the first attempt ran out and the team needs a full re-do rather than just a few extra minutes. If you only need a bit more time, use +time instead.

## Add Time (+1 / +2 / +5)

The +time buttons add extra minutes to the current timer. If the timer is running, the countdown extends seamlessly. If the timer is paused, the remaining time increases but the timer stays paused — useful when you want to bank extra time before resuming. If the timer has already expired (showing 00:00), adding time restarts the countdown from the added amount — so clicking +2 after expiry starts a 2-minute countdown. Choose the increment that matches the team's need: +1 for a quick wrap-up, +2 for a moderate extension, +5 when the discussion needs real breathing room. These increments are fixed and cannot be customized mid-retro.

## Mute Toggle

The mute toggle controls whether you hear the audio chime when the timer expires. This is a global personal setting stored in your browser — it applies across all sprints and teams, not just the current one. Muting on your device does not affect what other participants hear. By default, the sound is on so you notice when time runs out. If you find it distracting or you're in a shared space, mute it and rely on the visual flash (the timer turns red at 00:00). Your mute preference persists across sessions in the same browser.

## Timer Expired Alert

When the timer reaches zero, two things happen: the countdown display flashes red, and an audio chime plays (unless you've muted it). This is a signal to the team that the timebox is over — but it's not a hard stop. No actions are blocked, no inputs are disabled, and the board stays fully interactive. The Scrum Master decides what happens next: add more time if the team needs it, reset for a full redo, or advance to the next phase. Don't feel rushed to stop mid-thought — the timer is a guideline, and the SM has the final call on pacing.

## Timer Duration Defaults (Sprint Creation)

When creating a sprint, the Scrum Master can configure default timer durations for each timed phase: Writing (default 5 minutes), Voting (default 3 minutes), and Discussing (default 10 minutes). These values determine how long the countdown runs when the SM starts a timer during that phase. Valid values are 1 to 60 minutes. If you leave them at the defaults, that's fine — most retros work well with the standard timings. During the retro, the SM can always extend with +time or reset to the default, so these are starting points, not hard commitments.

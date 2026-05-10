# Team Management — Help Pages

Guide page content for UI elements. Each section explains interpretation and recommended actions.

---

## Team Dashboard

### Team Card — Sprint Count
This number shows how many sprints have been created under this team, regardless of their current state (draft, active, or closed). Use it to gauge how actively the team is running retrospectives. A team with zero sprints has been created but hasn't started any retros yet.

### Team Card — Last Sprint Date
This date shows when the most recent sprint was created. It helps you identify which teams are actively running retros and which may have gone dormant. If no date is shown, the team has no sprints yet.

### Create Team Button
Creates a new team that you own and manage as the Scrum Master. Each team is a separate workspace with its own sprints, participants, and retro history. You can create up to 10 teams — if you've reached the limit, delete a team you no longer need to free up a slot.

### Team Card — Three-Dot Menu
Opens options for managing this team. Currently, the available action is deleting the team. Deletion permanently removes the team and all its data, including every sprint, card, vote, and action item. This cannot be undone.

## Create Team Form

### Team Name Input
The team name is displayed on your dashboard and visible to participants when they join a sprint. Choose something your team will recognize — a project name, squad name, or group identifier. Names must be unique among your own teams, but other users can have teams with the same name. Maximum 100 characters.

## Delete Team Dialog

### Confirmation Name Input
To prevent accidental deletion, you must type the exact team name to confirm. This permanently deletes the team and everything inside it — all sprints, participants, cards, votes, evidence links, and action items. There is no way to recover deleted data. If the team has an active sprint (one that has been started but not closed), you must close or delete that sprint first.

### Why Can't I Delete This Team?
If the delete option shows a warning instead of a confirmation dialog, it means the team has at least one sprint in Active state — a retro that has been started but not yet closed. This safeguard prevents accidentally destroying a team while participants may be connected. To proceed, navigate to the team's sprint list, close or delete the active sprint, then return to the dashboard to delete the team.

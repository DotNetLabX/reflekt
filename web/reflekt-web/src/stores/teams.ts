import { defineStore } from 'pinia';

export interface TeamSummary {
  id: number;
  name: string;
  createdAt: string;
  sprintCount: number;
  lastSprintDate: string | null;
  hasActiveSprint: boolean;
}

let teamsRequest = 0;

export const useTeamsStore = defineStore('teams', {
  state: () => ({
    teams: [] as TeamSummary[],
    loaded: false,
    loading: false,
    error: null as string | null,
  }),
  getters: {
    hasTeams: (s) => s.teams.length > 0,
    isAtLimit: (s) => s.teams.length >= 10,
    teamById: (s) => (id: number) => s.teams.find((t) => t.id === id) ?? null,
  },
  actions: {
    async fetchMyTeams() {
      const request = ++teamsRequest;
      this.teams = [];
      this.error = null;
      this.loading = true;
      try {
        const res = await fetch('/api/teams', { credentials: 'include' });
        if (!res.ok) {
          if (request === teamsRequest) this.error = 'Failed to load teams';
          return;
        }
        const teams = (await res.json()) as TeamSummary[];
        if (request === teamsRequest) this.teams = teams;
      } catch {
        if (request === teamsRequest) this.error = "Can't reach the server";
      } finally {
        if (request === teamsRequest) {
          this.loading = false;
          this.loaded = true;
        }
      }
    },
    async createTeam(name: string): Promise<TeamSummary> {
      const res = await fetch('/api/teams', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name }),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw err;
      }
      const team = (await res.json()) as TeamSummary;
      this.teams.push(team);
      return team;
    },
    async deleteTeam(teamId: number, confirmationName: string): Promise<void> {
      const res = await fetch(`/api/teams/${teamId}`, {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmationName }),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw err;
      }
      this.teams = this.teams.filter((t) => t.id !== teamId);
    },
  },
});

import { defineStore } from 'pinia';

interface Team { id: number; name: string; createdAt: string; }

export const useTeamsStore = defineStore('teams', {
  state: () => ({
    teams: [] as Team[],
    loaded: false,
  }),
  getters: {
    hasTeam: (s) => s.teams.length > 0,
    currentTeam: (s) => s.teams[0] ?? null,
  },
  actions: {
    async fetchMyTeams() {
      const res = await fetch('/api/teams', { credentials: 'include' });
      this.teams = res.ok ? (await res.json()) as Team[] : [];
      this.loaded = true;
    },
    async createTeam(name: string): Promise<Team> {
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
      const team = (await res.json()) as Team;
      this.teams.push(team);
      return team;
    },
  },
});

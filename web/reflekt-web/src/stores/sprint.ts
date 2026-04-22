import { defineStore } from 'pinia'

interface Sprint {
  id: number
  teamId: number
  name: string
  state: string
  currentPhase: string
  votesPerParticipant: number
  createdAt: string
  closedAt: string | null
}

interface SprintSummary {
  id: number
  name: string
  currentPhase: string
  participants: { displayName: string }[]
}

export const useSprintStore = defineStore('sprint', {
  state: () => ({
    sprints: [] as Sprint[],
    currentSprint: null as Sprint | null,
    currentSummary: null as SprintSummary | null,
    loaded: false,
  }),
  actions: {
    async fetchTeamSprints(teamId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints`, { credentials: 'include' })
      this.sprints = res.ok ? ((await res.json()) as Sprint[]) : []
      this.loaded = true
    },
    async fetchSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}`, { credentials: 'include' })
      if (res.ok) {
        this.currentSprint = (await res.json()) as Sprint
      }
    },
    async fetchSprintSummary(sprintId: number) {
      const res = await fetch(`/api/sprints/${sprintId}/summary`)
      if (res.ok) {
        this.currentSummary = (await res.json()) as SprintSummary
      }
    },
    async createSprint(teamId: number, name: string, votesPerParticipant?: number): Promise<Sprint> {
      const res = await fetch(`/api/teams/${teamId}/sprints`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, votesPerParticipant }),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this.sprints.unshift(sprint)
      return sprint
    },
    async startSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/start`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    async advancePhase(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/advance-phase`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    async closeSprint(teamId: number, sprintId: number) {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/close`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
    },
    _replaceSprint(sprint: Sprint) {
      const idx = this.sprints.findIndex((s) => s.id === sprint.id)
      if (idx !== -1) this.sprints[idx] = sprint
      if (this.currentSprint?.id === sprint.id) this.currentSprint = sprint
    },
  },
})

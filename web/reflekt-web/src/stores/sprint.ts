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
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}

interface SprintSummary {
  id: number
  teamId: number
  name: string
  state: string
  currentPhase: string
  votesPerParticipant: number
  isScrumMaster: boolean
  participants: { displayName: string }[]
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}

export interface AdvancePhaseCheckResponse {
  currentPhase: string
  nonVotingParticipantCount?: number
  totalParticipantCount?: number
  discussedGroupCount?: number
  focusedGroupId?: number | null
  unreviewedActionCount?: number
  totalPreviousActionCount?: number
}

interface ColumnConfigPayload {
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
}

interface CreateSprintPayload {
  name: string
  votesPerParticipant?: number
  column1Name?: string
  column1Color?: string
  column2Name?: string
  column2Color?: string
  column3Name?: string
  column3Color?: string
  writingTimerDefault?: number
  votingTimerDefault?: number
  discussingTimerDefault?: number
}

const SERVER_UNREACHABLE = "Can't reach the server"

let sprintsRequest = 0
let currentSprintRequest = 0
let currentSummaryRequest = 0

export const useSprintStore = defineStore('sprint', {
  state: () => ({
    sprints: [] as Sprint[],
    currentSprint: null as Sprint | null,
    currentSummary: null as SprintSummary | null,
    loaded: false,
    sprintsLoading: false,
    sprintsError: null as string | null,
    currentSprintLoading: false,
    currentSprintError: null as string | null,
    currentSummaryLoading: false,
    currentSummaryError: null as string | null,
  }),
  actions: {
    async fetchTeamSprints(teamId: number) {
      const request = ++sprintsRequest
      this.sprints = []
      this.sprintsError = null
      this.sprintsLoading = true
      try {
        const res = await fetch(`/api/teams/${teamId}/sprints`, { credentials: 'include' })
        if (!res.ok) {
          if (request === sprintsRequest) this.sprintsError = 'Failed to load sprints'
          return
        }
        const sprints = (await res.json()) as Sprint[]
        if (request === sprintsRequest) this.sprints = sprints
      } catch {
        if (request === sprintsRequest) this.sprintsError = SERVER_UNREACHABLE
      } finally {
        if (request === sprintsRequest) {
          this.sprintsLoading = false
          this.loaded = true
        }
      }
    },
    async fetchSprint(teamId: number, sprintId: number) {
      const request = ++currentSprintRequest
      this.currentSprint = null
      this.currentSprintError = null
      this.currentSprintLoading = true
      try {
        const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}`, { credentials: 'include' })
        if (!res.ok) {
          if (request === currentSprintRequest) {
            this.currentSprintError = res.status === 404 ? 'Sprint not found' : 'Failed to load sprint'
          }
          return
        }
        const sprint = (await res.json()) as Sprint
        if (request === currentSprintRequest) this.currentSprint = sprint
      } catch {
        if (request === currentSprintRequest) this.currentSprintError = SERVER_UNREACHABLE
      } finally {
        if (request === currentSprintRequest) this.currentSprintLoading = false
      }
    },
    async fetchSprintSummary(sprintId: number) {
      const request = ++currentSummaryRequest
      this.currentSummary = null
      this.currentSummaryError = null
      this.currentSummaryLoading = true
      try {
        const res = await fetch(`/api/sprints/${sprintId}/summary`)
        if (!res.ok) {
          if (request === currentSummaryRequest) {
            this.currentSummaryError = res.status === 404 ? 'Sprint not found' : 'Failed to load sprint'
          }
          return
        }
        const summary = (await res.json()) as SprintSummary
        if (request === currentSummaryRequest) this.currentSummary = summary
      } catch {
        if (request === currentSummaryRequest) this.currentSummaryError = SERVER_UNREACHABLE
      } finally {
        if (request === currentSummaryRequest) this.currentSummaryLoading = false
      }
    },
    async createSprint(teamId: number, payload: CreateSprintPayload): Promise<Sprint> {
      const res = await fetch(`/api/teams/${teamId}/sprints`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this.sprints.unshift(sprint)
      return sprint
    },
    async updateColumns(sprintId: number, payload: ColumnConfigPayload): Promise<Sprint> {
      const res = await fetch(`/api/sprints/${sprintId}/columns`, {
        method: 'PUT',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      const sprint = (await res.json()) as Sprint
      this._replaceSprint(sprint)
      if (this.currentSummary?.id === sprint.id) {
        this.currentSummary.column1Name = sprint.column1Name
        this.currentSummary.column1Color = sprint.column1Color
        this.currentSummary.column2Name = sprint.column2Name
        this.currentSummary.column2Color = sprint.column2Color
        this.currentSummary.column3Name = sprint.column3Name
        this.currentSummary.column3Color = sprint.column3Color
      }
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
    async advancePhaseCheck(teamId: number, sprintId: number): Promise<AdvancePhaseCheckResponse> {
      const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/advance-phase/check`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
      })
      if (!res.ok) throw await res.json().catch(() => ({}))
      return (await res.json()) as AdvancePhaseCheckResponse
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

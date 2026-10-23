import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

interface Participant {
  id: number
  sprintId: number
  displayName: string
  joinedAt: string
}

interface JoinSprintResponse extends Participant {
  guestToken: string
  userId?: number | null
}

let meRequest = 0
let participantsRequest = 0
// Shared by every action that sets currentParticipant: a response that lands after a
// later identity action started, or after the room abandoned its pending ones, is dropped.
let identityRequest = 0

export const useParticipantStore = defineStore('participant', {
  state: () => ({
    currentParticipant: null as Participant | null,
    participants: [] as Participant[],
    meLoading: false,
    meError: null as string | null,
    meStatus: null as number | null,
    participantsLoading: false,
    participantsError: null as string | null,
  }),
  actions: {
    async join(sprintId: number, displayName: string): Promise<JoinSprintResponse> {
      const identity = ++identityRequest
      const res = await fetch(`/api/sprints/${sprintId}/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName }),
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const data = (await res.json()) as JoinSprintResponse
      if (identity === identityRequest) this.currentParticipant = data
      return data
    },
    async joinWithGoogle(sprintId: number): Promise<JoinSprintResponse> {
      const identity = ++identityRequest
      const res = await fetch(`/api/sprints/${sprintId}/join/google`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{}',
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      const data = (await res.json()) as JoinSprintResponse
      if (identity === identityRequest) this.currentParticipant = data
      return data
    },
    abandonPendingIdentity(): void {
      identityRequest++
    },
    async fetchMe(sprintId: number): Promise<void> {
      const request = ++meRequest
      const identity = ++identityRequest
      this.currentParticipant = null
      this.meError = null
      this.meStatus = null
      this.meLoading = true
      try {
        const guestStore = useGuestStore()
        const res = await fetch(`/api/sprints/${sprintId}/participants/me`, {
          headers: guestStore.token ? { 'X-Guest-Token': guestStore.token } : {},
        })
        if (!res.ok) {
          if (request === meRequest) {
            this.meError = 'Failed to load your place in this retro'
            this.meStatus = res.status
          }
          return
        }
        const participant = (await res.json()) as Participant
        if (identity === identityRequest) this.currentParticipant = participant
      } catch {
        if (request === meRequest) this.meError = "Can't reach the server"
      } finally {
        if (request === meRequest) this.meLoading = false
      }
    },
    async fetchParticipants(sprintId: number): Promise<void> {
      const request = ++participantsRequest
      this.participants = []
      this.participantsError = null
      this.participantsLoading = true
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/sprints/${sprintId}/participants`, { headers, credentials: 'include' })
        if (!res.ok) {
          if (request === participantsRequest) this.participantsError = 'Failed to load participants'
          return
        }
        const participants = (await res.json()) as Participant[]
        if (request === participantsRequest) this.participants = participants
      } catch {
        if (request === participantsRequest) this.participantsError = "Can't reach the server"
      } finally {
        if (request === participantsRequest) this.participantsLoading = false
      }
    },
    async kickParticipant(sprintId: number, participantId: number): Promise<void> {
      const res = await fetch(`/api/sprints/${sprintId}/participants/${participantId}`, {
        method: 'DELETE',
        credentials: 'include',
      })
      if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
      this.participants = this.participants.filter((p) => p.id !== participantId)
    },
  },
})

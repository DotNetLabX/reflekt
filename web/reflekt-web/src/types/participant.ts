export interface Participant {
  id: number
  sprintId: number
  displayName: string
  joinedAt: string
}

export type ParticipantName = Pick<Participant, 'id' | 'displayName'>

export interface JoinSprintResponse extends Participant {
  guestToken: string
  userId?: number | null
}

export interface ReadinessSnapshot {
  readyParticipantIds: number[]
  readyCount: number
  totalCount: number
}

export interface ReadinessChangedPayload {
  participantId: number
  displayName: string | null
  isReady: boolean
  readyCount: number
  totalCount: number
}

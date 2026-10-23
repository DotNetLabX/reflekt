import type { ColumnColor, SprintPhase, SprintState } from './domain'

export interface ColumnConfig {
  column1Name: string
  column1Color: ColumnColor
  column2Name: string
  column2Color: ColumnColor
  column3Name: string
  column3Color: ColumnColor
}

export interface TimerDefaults {
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}

export interface Sprint extends ColumnConfig, TimerDefaults {
  id: number
  teamId: number
  name: string
  state: SprintState
  currentPhase: SprintPhase
  votesPerParticipant: number
  createdAt: string
  closedAt: string | null
}

export interface SprintSummary extends ColumnConfig, TimerDefaults {
  id: number
  teamId: number
  name: string
  state: SprintState
  currentPhase: SprintPhase
  votesPerParticipant: number
  isScrumMaster: boolean
  participants: { displayName: string }[]
}

export interface AdvancePhaseCheckResponse {
  currentPhase: SprintPhase
  nonVotingParticipantCount?: number
  totalParticipantCount?: number
  discussedGroupCount?: number
  focusedGroupId?: number | null
  unreviewedActionCount?: number
  totalPreviousActionCount?: number
}

export type ColumnConfigPayload = ColumnConfig

export interface ColumnConfigUpdatedPayload extends ColumnConfig {
  sprintId: number
}

export interface CreateSprintPayload extends Partial<ColumnConfig>, Partial<TimerDefaults> {
  name: string
  votesPerParticipant?: number
}

export interface PhaseActionsEntry {
  phase: SprintPhase
  actions: string[]
}

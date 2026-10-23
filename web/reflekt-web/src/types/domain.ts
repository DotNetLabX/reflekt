import type { TimerDefaults } from './sprint'

export type SprintPhase = 'Writing' | 'Grouping' | 'Voting' | 'Discussing' | 'Reporting'

export type SprintState = 'Draft' | 'Active' | 'Closed'

export type ColumnType = 'Column1' | 'Column2' | 'Column3' | 'ActionItems'

export type CardType = 'Note' | 'ActionItem'

export type ReviewStatus = 'None' | 'Done' | 'CarriedOver' | 'Dropped'

export type FetchStatus = 'Pending' | 'Ok' | 'Failed'

export type ColumnColor = 'green' | 'red' | 'amber' | 'blue' | 'purple' | 'teal' | 'pink' | 'slate'

export interface PhaseInfo {
  name: SprintPhase
  short: string
  next: SprintPhase | null
  timed: boolean
  timerDefault: keyof TimerDefaults | null
  readiness: boolean
}

export const PHASES: readonly PhaseInfo[] = [
  {
    name: 'Writing',
    short: 'Write',
    next: 'Grouping',
    timed: true,
    timerDefault: 'writingTimerDefault',
    readiness: true,
  },
  {
    name: 'Grouping',
    short: 'Group',
    next: 'Voting',
    timed: false,
    timerDefault: null,
    readiness: false,
  },
  {
    name: 'Voting',
    short: 'Vote',
    next: 'Discussing',
    timed: true,
    timerDefault: 'votingTimerDefault',
    readiness: true,
  },
  {
    name: 'Discussing',
    short: 'Discuss',
    next: 'Reporting',
    timed: true,
    timerDefault: 'discussingTimerDefault',
    readiness: false,
  },
  {
    name: 'Reporting',
    short: 'Report',
    next: null,
    timed: false,
    timerDefault: null,
    readiness: false,
  },
]

import type { ColumnColor, ColumnType } from '@/types/domain'
import type { TimerDefaults } from '@/types/sprint'

export const PALETTE_COLORS: readonly ColumnColor[] = [
  'green',
  'red',
  'amber',
  'blue',
  'purple',
  'teal',
  'pink',
  'slate',
]

interface ColumnDefault {
  name: string
  color: ColumnColor
}

export const DEFAULT_COLUMNS: readonly [ColumnDefault, ColumnDefault, ColumnDefault] = [
  { name: 'Went Well', color: 'green' },
  { name: "Didn't Go Well", color: 'red' },
  { name: 'Start Doing', color: 'amber' },
]

export const DEFAULT_TIMERS: Readonly<TimerDefaults> = {
  writingTimerDefault: 5,
  votingTimerDefault: 3,
  discussingTimerDefault: 10,
}

export function columnTabId(column: ColumnType): string {
  return `retro-tab-${column}`
}

export function columnPanelId(column: ColumnType): string {
  return `retro-panel-${column}`
}

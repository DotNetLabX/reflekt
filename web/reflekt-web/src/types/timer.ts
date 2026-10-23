export type TimerStatus = 'running' | 'paused' | 'expired' | 'stopped'

export interface TimerPayload {
  status: TimerStatus
  remainingSeconds: number
  endTimeUtc: string | null
}

export type TimerMethod = 'PauseTimer' | 'ResumeTimer' | 'ResetTimer'

export interface TeamSummary {
  id: number
  name: string
  createdAt: string
  sprintCount: number
  lastSprintDate: string | null
  hasActiveSprint: boolean
}

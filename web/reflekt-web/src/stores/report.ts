import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface ReportEvidenceLinkEntry {
  url: string
  source: string
  title: string | null
  externalKey: string | null
}

export interface ReportCardEntry {
  text: string
  evidenceLinks: ReportEvidenceLinkEntry[]
}

export interface ReportGroupEntry {
  groupTitle: string
  voteCount: number
  isDiscussed: boolean
  cards: ReportCardEntry[]
}

export interface ReportActionItemEntry {
  text: string
  assigneeName: string | null
  evidenceLinks: ReportEvidenceLinkEntry[]
}

export interface ReportColumnSection {
  columnName: string
  columnColor: string
  groups: ReportGroupEntry[]
}

export interface ReportSprintInfo {
  id: number
  name: string
  createdAt: string
  closedAt: string | null
}

export interface ReportTeamInfo {
  id: number
  name: string
}

export interface ReportParticipantInfo {
  displayName: string
}

export interface ReportPreviousActionEntry {
  text: string
  assigneeName: string | null
  reviewStatus: string
}

export interface ReportPreviousActionsSection {
  previousSprintName: string
  items: ReportPreviousActionEntry[]
}

export interface ReportResponse {
  sprint: ReportSprintInfo
  team: ReportTeamInfo
  participants: ReportParticipantInfo[]
  columns: ReportColumnSection[]
  actionItems: ReportActionItemEntry[]
  previousActionsReview?: ReportPreviousActionsSection | null
}

let reportRequest = 0

function reportFailureMessage(status: number): string {
  if (status === 404) return 'Sprint not found'
  if (status === 401 || status === 403) return 'Not authorized'
  return 'Failed to load report'
}

export const useReportStore = defineStore('report', {
  state: () => ({
    report: null as ReportResponse | null,
    loading: false,
    error: null as string | null,
  }),
  actions: {
    async fetchReport(teamId: number, sprintId: number): Promise<void> {
      const request = this._startReportLoad()
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/report`, {
          headers,
          credentials: 'include',
        })
        await this._commitReport(request, res)
      } catch {
        if (request === reportRequest) this.error = "Can't reach the server"
      } finally {
        if (request === reportRequest) this.loading = false
      }
    },

    async fetchParticipantReport(sprintId: number): Promise<void> {
      const request = this._startReportLoad()
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/sprints/${sprintId}/report`, {
          headers,
          credentials: 'include',
        })
        await this._commitReport(request, res)
      } catch {
        if (request === reportRequest) this.error = "Can't reach the server"
      } finally {
        if (request === reportRequest) this.loading = false
      }
    },

    _startReportLoad(): number {
      this.report = null
      this.error = null
      this.loading = true
      return ++reportRequest
    },

    async _commitReport(request: number, res: Response): Promise<void> {
      if (!res.ok) {
        if (request === reportRequest) this.error = reportFailureMessage(res.status)
        return
      }
      const report = (await res.json()) as ReportResponse
      if (request === reportRequest) this.report = report
    },

    async copyAsMarkdown(): Promise<boolean> {
      if (!this.report) return false
      const r = this.report
      const date = new Date(r.sprint.createdAt).toLocaleDateString()
      const participants = r.participants.map((p) => p.displayName).join(', ')

      const lines: string[] = []
      lines.push(`# ${r.sprint.name} — Sprint Report`)
      lines.push('')
      lines.push(`**Team:** ${r.team.name}`)
      lines.push(`**Date:** ${date}`)
      lines.push(`**Participants:** ${participants}`)
      lines.push('')
      for (const column of r.columns) {
        lines.push(`## ${column.columnName}`)
        lines.push('')
        for (const group of column.groups) {
          const discussed = group.isDiscussed ? ' [discussed]' : ''
          lines.push(`### ${group.groupTitle} (${group.voteCount} votes)${discussed}`)
          for (const card of group.cards) {
            lines.push(`- ${card.text}`)
            for (const link of card.evidenceLinks) {
              const label = link.title || link.url
              lines.push(`  - [${label}](${link.url})`)
            }
          }
        }
        lines.push('')
      }
      lines.push('## Action Items')
      lines.push('')
      for (const item of r.actionItems) {
        const assignee = item.assigneeName ? ` — @${item.assigneeName}` : ''
        lines.push(`- [ ] ${item.text}${assignee}`)
        for (const link of item.evidenceLinks) {
          const label = link.title || link.url
          lines.push(`  - [${label}](${link.url})`)
        }
      }

      if (r.previousActionsReview) {
        lines.push('')
        lines.push(`## Previous Actions Review (from: ${r.previousActionsReview.previousSprintName})`)
        lines.push('')
        for (const entry of r.previousActionsReview.items) {
          const assignee = entry.assigneeName ? ` — @${entry.assigneeName}` : ''
          const status = entry.reviewStatus === 'CarriedOver' ? 'Carried Over' : entry.reviewStatus
          lines.push(`- [${status}] ${entry.text}${assignee}`)
        }
      }

      await navigator.clipboard.writeText(lines.join('\n'))
      return true
    },
  },
})

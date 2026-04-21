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

export interface ReportActionItemEntry {
  text: string
  assigneeName: string | null
  evidenceLinks: ReportEvidenceLinkEntry[]
}

export interface ReportNotesSection {
  wentWell: ReportCardEntry[]
  didntGoWell: ReportCardEntry[]
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

export interface ReportResponse {
  sprint: ReportSprintInfo
  team: ReportTeamInfo
  participants: ReportParticipantInfo[]
  notes: ReportNotesSection
  actionItems: ReportActionItemEntry[]
}

export const useReportStore = defineStore('report', {
  state: () => ({
    report: null as ReportResponse | null,
    loading: false,
    error: null as string | null,
  }),
  actions: {
    async fetchReport(teamId: number, sprintId: number): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/teams/${teamId}/sprints/${sprintId}/report`, {
          headers,
          credentials: 'include',
        })
        if (res.status === 404) {
          this.error = 'Sprint not found'
          return
        }
        if (res.status === 401 || res.status === 403) {
          this.error = 'Not authorized'
          return
        }
        if (!res.ok) {
          this.error = 'Failed to load report'
          return
        }
        this.report = (await res.json()) as ReportResponse
      } finally {
        this.loading = false
      }
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
      lines.push('## Went Well')
      lines.push('')
      for (const card of r.notes.wentWell) {
        lines.push(`- ${card.text}`)
        for (const link of card.evidenceLinks) {
          const label = link.title || link.url
          lines.push(`  - [${label}](${link.url})`)
        }
      }
      lines.push('')
      lines.push("## Didn't Go Well")
      lines.push('')
      for (const card of r.notes.didntGoWell) {
        lines.push(`- ${card.text}`)
        for (const link of card.evidenceLinks) {
          const label = link.title || link.url
          lines.push(`  - [${label}](${link.url})`)
        }
      }
      lines.push('')
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

      await navigator.clipboard.writeText(lines.join('\n'))
      return true
    },
  },
})

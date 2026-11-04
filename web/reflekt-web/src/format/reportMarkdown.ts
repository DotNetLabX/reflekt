import { formatDate, formatStatus, formatVotes } from './format'
import type { ReportEvidenceLinkEntry, ReportResponse } from '@/types/report'

function linkLines(links: ReportEvidenceLinkEntry[]): string[] {
  return links.map((link) => `  - [${link.title || link.url}](${link.url})`)
}

export function reportMarkdown(r: ReportResponse): string {
  const participants = r.participants.map((p) => p.displayName).join(', ')

  const lines: string[] = [
    `# ${r.sprint.name} — Sprint Report`,
    '',
    `**Team:** ${r.team.name}`,
    `**Date:** ${formatDate(r.sprint.createdAt)}`,
    `**Participants:** ${participants}`,
    '',
  ]
  for (const column of r.columns) {
    lines.push(`## ${column.columnName}`, '')
    for (const group of column.groups) {
      const discussed = group.isDiscussed ? ' [discussed]' : ''
      lines.push(`### ${group.groupTitle} (${formatVotes(group.voteCount)})${discussed}`)
      for (const card of group.cards) {
        lines.push(`- ${card.text}`, ...linkLines(card.evidenceLinks))
      }
    }
    lines.push('')
  }
  lines.push('## Action Items', '')
  for (const item of r.actionItems) {
    const assignee = item.assigneeName ? ` — @${item.assigneeName}` : ''
    lines.push(`- [ ] ${item.text}${assignee}`, ...linkLines(item.evidenceLinks))
  }

  if (r.previousActionsReview) {
    lines.push(
      '',
      `## Previous Actions Review (from: ${r.previousActionsReview.previousSprintName})`,
      '',
    )
    for (const entry of r.previousActionsReview.items) {
      const assignee = entry.assigneeName ? ` — @${entry.assigneeName}` : ''
      lines.push(`- [${formatStatus(entry.reviewStatus)}] ${entry.text}${assignee}`)
    }
  }

  return lines.join('\n')
}

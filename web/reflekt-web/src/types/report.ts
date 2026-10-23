import type { ColumnColor, ReviewStatus } from './domain'

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
  columnColor: ColumnColor
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

export type ReportReviewStatus = ReviewStatus | 'Not reviewed'

export interface ReportPreviousActionEntry {
  text: string
  assigneeName: string | null
  reviewStatus: ReportReviewStatus
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

export interface PreviousActionItem {
  cardId: number
  text: string
  assigneeName: string | null
  reviewStatus: ReviewStatus
  previousSprintId: number
  previousSprintName: string
}

export interface ReviewStatusChangedPayload {
  cardId: number
  reviewStatus: ReviewStatus
  reviewedCount: number
  totalCount: number
}

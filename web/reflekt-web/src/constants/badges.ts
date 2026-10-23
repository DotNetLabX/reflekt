import type { SprintState } from '@/types/domain'
import type { ReportReviewStatus } from '@/types/report'
import type { BadgeVariant } from '@/types/ui'

export const SPRINT_STATE_BADGE: Record<SprintState, BadgeVariant> = {
  Draft: 'default',
  Active: 'success',
  Closed: 'danger',
}

export const REVIEW_STATUS_BADGE: Record<ReportReviewStatus, BadgeVariant> = {
  Done: 'success',
  CarriedOver: 'warning',
  Dropped: 'default',
  None: 'danger',
  'Not reviewed': 'danger',
}

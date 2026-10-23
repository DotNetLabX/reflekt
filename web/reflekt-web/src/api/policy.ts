import { apiFetch } from './client'
import type { PhaseActionsEntry } from '@/types/sprint'

export const policyApi = {
  phaseActions: () => apiFetch<PhaseActionsEntry[]>('/api/phase-actions'),
}

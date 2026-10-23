import { apiFetch } from './client'
import type { Card, CreateCardPayload, EditCardPayload, EvidenceLink } from '@/types/card'

export const cardsApi = {
  list: (sprintId: number) => apiFetch<Card[]>(`/api/sprints/${sprintId}/cards`, { guest: true }),
  create: (sprintId: number, payload: CreateCardPayload) =>
    apiFetch<Card>(`/api/sprints/${sprintId}/cards`, {
      method: 'POST',
      body: payload,
      guest: true,
    }),
  edit: (sprintId: number, cardId: number, payload: EditCardPayload) =>
    apiFetch<Card>(`/api/sprints/${sprintId}/cards/${cardId}`, {
      method: 'PUT',
      body: payload,
      guest: true,
    }),
  remove: (sprintId: number, cardId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/cards/${cardId}`, { method: 'DELETE', guest: true }),
  attachEvidenceLink: (sprintId: number, cardId: number, url: string) =>
    apiFetch<EvidenceLink>(`/api/sprints/${sprintId}/cards/${cardId}/evidence-links`, {
      method: 'POST',
      body: { url },
      guest: true,
    }),
  detachEvidenceLink: (sprintId: number, cardId: number, evidenceLinkId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/cards/${cardId}/evidence-links/${evidenceLinkId}`, {
      method: 'DELETE',
      guest: true,
    }),
}

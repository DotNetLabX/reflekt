import { apiFetch } from './client'
import type { CardGroup } from '@/types/card'

export const cardGroupsApi = {
  list: (sprintId: number) =>
    apiFetch<CardGroup[]>(`/api/sprints/${sprintId}/card-groups`, { guest: true }),
  create: (sprintId: number, cardIds: number[], title?: string) =>
    apiFetch<CardGroup>(`/api/sprints/${sprintId}/card-groups`, {
      method: 'POST',
      body: { cardIds, title },
      guest: true,
    }),
  addCard: (sprintId: number, groupId: number, cardId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}/cards`, {
      method: 'POST',
      body: { cardId },
      guest: true,
    }),
  removeCard: (sprintId: number, groupId: number, cardId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}/cards/${cardId}`, {
      method: 'DELETE',
      guest: true,
    }),
  dissolve: (sprintId: number, groupId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}`, {
      method: 'DELETE',
      guest: true,
    }),
  focus: (sprintId: number, groupId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}/focus`, {
      method: 'POST',
      body: {},
      guest: true,
    }),
  toggleDiscussed: (sprintId: number, groupId: number) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}/toggle-discussed`, {
      method: 'POST',
      body: {},
      guest: true,
    }),
  updateTitle: (sprintId: number, groupId: number, title: string | null) =>
    apiFetch<void>(`/api/sprints/${sprintId}/card-groups/${groupId}`, {
      method: 'PATCH',
      body: { title },
      guest: true,
    }),
}

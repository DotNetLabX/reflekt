import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { cardGroupsApi } from '@/api/cardGroups'
import { isHttpError } from '@/api/client'
import { createPendingValues } from './pendingValues'
import { useVoteStore } from './vote'
import type { CardGroup } from '@/types/card'

export const useCardGroupStore = defineStore('cardGroup', () => {
  const voteStore = useVoteStore()

  const groups = ref<CardGroup[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const column1Groups = computed(() => groups.value.filter((g) => g.columnType === 'Column1'))
  const column2Groups = computed(() => groups.value.filter((g) => g.columnType === 'Column2'))
  const column3Groups = computed(() => groups.value.filter((g) => g.columnType === 'Column3'))

  let groupsRequest = 0

  function groupSetter<K extends 'title' | 'isDiscussed' | 'isFocused'>(field: K) {
    return (groupId: number, value: CardGroup[K]) => {
      const group = groups.value.find((g) => g.id === groupId)
      if (group) group[field] = value
    }
  }

  const pendingTitles = createPendingValues(groupSetter('title'))
  const pendingDiscussed = createPendingValues(groupSetter('isDiscussed'))
  const pendingFocus = createPendingValues(groupSetter('isFocused'))

  function settleAll(): void {
    pendingTitles.settleAll()
    pendingDiscussed.settleAll()
    pendingFocus.settleAll()
  }

  // A background refresh keeps the groups on screen and keeps a previous refresh's
  // error until one succeeds.
  async function fetchGroups(sprintId: number, { background = false } = {}): Promise<void> {
    const request = ++groupsRequest
    if (!background) {
      groups.value = []
      error.value = null
      loading.value = true
    }
    try {
      const result = await cardGroupsApi.list(sprintId)
      if (request !== groupsRequest) return
      groups.value = result
      error.value = null
      settleAll()
      voteStore.initVoteCounts(result.map((g) => ({ groupId: g.id, voteCount: g.voteCount })))
    } catch (e) {
      if (request === groupsRequest) {
        error.value = isHttpError(e) ? 'Failed to load groups' : "Can't reach the server"
      }
    } finally {
      if (request === groupsRequest) loading.value = false
    }
  }

  async function createGroup(
    sprintId: number,
    cardIds: number[],
    title?: string,
  ): Promise<CardGroup> {
    const group = await cardGroupsApi.create(sprintId, cardIds, title)
    await fetchGroups(sprintId, { background: true })
    return group
  }

  async function addCardToGroup(sprintId: number, groupId: number, cardId: number): Promise<void> {
    await cardGroupsApi.addCard(sprintId, groupId, cardId)
    await fetchGroups(sprintId, { background: true })
  }

  async function removeCardFromGroup(
    sprintId: number,
    groupId: number,
    cardId: number,
  ): Promise<void> {
    await cardGroupsApi.removeCard(sprintId, groupId, cardId)
    await fetchGroups(sprintId, { background: true })
  }

  async function dissolveGroup(sprintId: number, groupId: number): Promise<void> {
    await cardGroupsApi.dissolve(sprintId, groupId)
    await fetchGroups(sprintId, { background: true })
  }

  async function focusGroup(sprintId: number, groupId: number): Promise<void> {
    const changes = groups.value
      .filter((g) => g.isFocused !== (g.id === groupId))
      .map((g) => ({ groupId: g.id, change: pendingFocus.change(g.id, g.isFocused, !g.isFocused) }))
    try {
      await cardGroupsApi.focus(sprintId, groupId)
      changes.forEach((c) => pendingFocus.confirm(c.groupId, c.change))
    } catch (e) {
      changes.forEach((c) => pendingFocus.undo(c.groupId, c.change))
      throw e
    }
  }

  async function toggleDiscussed(sprintId: number, groupId: number): Promise<void> {
    const group = groups.value.find((g) => g.id === groupId)
    const change = group
      ? pendingDiscussed.change(groupId, group.isDiscussed, !group.isDiscussed)
      : null
    try {
      await cardGroupsApi.toggleDiscussed(sprintId, groupId)
      if (change) pendingDiscussed.confirm(groupId, change)
    } catch (e) {
      if (change) pendingDiscussed.undo(groupId, change)
      throw e
    }
  }

  async function updateGroupTitle(
    sprintId: number,
    groupId: number,
    title: string | null,
  ): Promise<void> {
    const group = groups.value.find((g) => g.id === groupId)
    const change = group ? pendingTitles.change(groupId, group.title, title) : null
    try {
      await cardGroupsApi.updateTitle(sprintId, groupId, title)
      if (change) pendingTitles.confirm(groupId, change)
    } catch (e) {
      if (change) pendingTitles.undo(groupId, change)
      throw e
    }
  }

  function applyTitleChanged(groupId: number, title: string | null): void {
    pendingTitles.settle(groupId)
    const group = groups.value.find((g) => g.id === groupId)
    if (group) group.title = title
  }

  function applyGroupFocused(groupId: number): void {
    pendingFocus.settleAll()
    groups.value.forEach((g) => (g.isFocused = g.id === groupId))
  }

  function applyFocusCleared(): void {
    pendingFocus.settleAll()
    groups.value.forEach((g) => (g.isFocused = false))
  }

  function applyDiscussedChanged(groupId: number, isDiscussed: boolean): void {
    pendingDiscussed.settle(groupId)
    const group = groups.value.find((g) => g.id === groupId)
    if (group) group.isDiscussed = isDiscussed
  }

  return {
    groups,
    loading,
    error,
    column1Groups,
    column2Groups,
    column3Groups,
    fetchGroups,
    createGroup,
    addCardToGroup,
    removeCardFromGroup,
    dissolveGroup,
    focusGroup,
    toggleDiscussed,
    updateGroupTitle,
    applyTitleChanged,
    applyGroupFocused,
    applyFocusCleared,
    applyDiscussedChanged,
  }
})

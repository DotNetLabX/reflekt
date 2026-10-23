import { computed, ref, type InjectionKey } from 'vue'
import { DEFAULT_COLUMNS } from '@/constants/columns'
import { useCardStore } from '@/stores/card'
import { useParticipantStore } from '@/stores/participant'
import { useAuthStore } from '@/stores/auth'
import { useVoteStore } from '@/stores/vote'
import { useSprintStore } from '@/stores/sprint'
import { usePolicyStore } from '@/stores/policy'
import { useCardGroupStore } from '@/stores/cardGroup'
import { usePreviousActionsStore } from '@/stores/previousActions'
import { useRetroPhase } from './useRetroPhase'
import type { Card, CardGroup, DragPayload } from '@/types/card'
import type { ColumnColor, ColumnType } from '@/types/domain'

type NoteColumnType = 'Column1' | 'Column2' | 'Column3'

export const boardActionsKey: InjectionKey<ReturnType<typeof useRetroBoardActions>['actions']> =
  Symbol('boardActions')

export function missingBoardActions(component: string): never {
  throw new Error(`${component} must be rendered inside RetroBoardPanel`)
}

export function useRetroBoardActions(sprintId: number, onError: (msg: string) => void) {
  const cardStore = useCardStore()
  const participantStore = useParticipantStore()
  const authStore = useAuthStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const policyStore = usePolicyStore()
  const cardGroupStore = useCardGroupStore()
  const previousActionsStore = usePreviousActionsStore()

  // --- Board state ---

  const participants = computed(() => participantStore.participants)
  const actionItems = computed(() => cardStore.actionItemCards)
  const {
    currentPhase: phase,
    isWriting: isWritingPhase,
    isDiscussing: isDiscussingPhase,
    isReporting,
    showGroups,
    isClosed,
    isScrumMaster,
    showReadiness,
  } = useRetroPhase()
  const currentPhase = computed(() => phase.value ?? '')
  const currentParticipantId = computed(() => participantStore.currentParticipant?.id ?? 0)
  const canGroup = computed(() => policyStore.isAllowed(currentPhase.value, 'GroupCards'))
  const canUngroup = computed(() => policyStore.isAllowed(currentPhase.value, 'UngroupCards'))
  const canCreateNote = computed(() => policyStore.isAllowed(currentPhase.value, 'CreateNote'))
  const canCreateActionItem = computed(() =>
    policyStore.isAllowed(currentPhase.value, 'CreateActionItem'),
  )
  const showActionItems = computed(() => canCreateActionItem.value || isReporting.value)
  const showPreviousActions = computed(
    () => isWritingPhase.value && previousActionsStore.items.length > 0,
  )
  const teamId = computed(() => sprintStore.currentSummary?.teamId ?? 0)
  const boardGridClasses = computed(() =>
    showActionItems.value
      ? 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 flex-1 min-h-0 grid-rows-[1fr]'
      : 'grid grid-cols-1 sm:grid-cols-3 gap-4 flex-1 min-h-0 grid-rows-[1fr]',
  )

  function sortedByVotes(groups: CardGroup[]): CardGroup[] {
    return [...groups].sort(
      (a, b) => voteStore.voteCountForGroup(b.id) - voteStore.voteCountForGroup(a.id),
    )
  }

  function noteColumn(
    type: NoteColumnType,
    name: string,
    color: ColumnColor,
    badge: 'success' | 'danger' | 'primary',
    cards: Card[],
    groups: CardGroup[],
  ) {
    return { type, name, color, badge, cards, groups: sortedByVotes(groups) }
  }

  const noteColumns = computed(() => {
    const summary = sprintStore.currentSummary
    const [default1, default2, default3] = DEFAULT_COLUMNS
    return [
      noteColumn(
        'Column1',
        summary?.column1Name ?? default1.name,
        summary?.column1Color ?? default1.color,
        'success',
        cardStore.column1Cards,
        cardGroupStore.column1Groups,
      ),
      noteColumn(
        'Column2',
        summary?.column2Name ?? default2.name,
        summary?.column2Color ?? default2.color,
        'danger',
        cardStore.column2Cards,
        cardGroupStore.column2Groups,
      ),
      noteColumn(
        'Column3',
        summary?.column3Name ?? default3.name,
        summary?.column3Color ?? default3.color,
        'primary',
        cardStore.column3Cards,
        cardGroupStore.column3Groups,
      ),
    ] as const
  })

  const deleteTarget = ref<Card | null>(null)

  // --- Prop builders ---

  function canMutateCard(card: Card): boolean {
    if (cardStore.isPendingCard(card)) return false
    if (card.participantId !== participantStore.currentParticipant?.id) return false
    if (isClosed.value) return false
    const isActionItem = card.cardType === 'ActionItem'
    if (!policyStore.isAllowed(currentPhase.value, isActionItem ? 'EditActionItem' : 'EditNote'))
      return false
    return !(isActionItem && isDiscussingPhase.value && !authStore.isSignedIn)
  }

  function cardItemProps(card: Card, color: ColumnColor) {
    return {
      card,
      canEdit: canMutateCard(card),
      blurred: isWritingPhase.value && card.participantId !== currentParticipantId.value,
      canAttachLink:
        !cardStore.isPendingCard(card) &&
        policyStore.isAllowed(currentPhase.value, 'AttachEvidenceLink'),
      columnColor: color,
    }
  }

  function groupItemProps(group: CardGroup, color: ColumnColor) {
    return {
      group,
      columnColor: color,
      showVoting: policyStore.isAllowed(currentPhase.value, 'CastVote'),
      voteCount: voteStore.voteCountForGroup(group.id),
      myVoteCount: voteStore.myVotesOnGroup(group.id),
      remainingBudget: voteStore.remainingBudget,
      isScrumMaster: isScrumMaster.value,
      canGroup: canGroup.value,
      canUngroup: canUngroup.value,
      isDiscussing: isDiscussingPhase.value,
    }
  }

  // --- Mutation handlers ---

  async function addNote(column: NoteColumnType, text: string): Promise<boolean> {
    try {
      await cardStore.createCard(sprintId, { text, columnType: column, cardType: 'Note' })
      return true
    } catch {
      onError('Failed to add card.')
      return false
    }
  }

  async function addActionItem(text: string, assigneeName: string): Promise<boolean> {
    try {
      await cardStore.createCard(sprintId, {
        text,
        columnType: 'ActionItems',
        cardType: 'ActionItem',
        assigneeName,
      })
      return true
    } catch {
      onError('Failed to add action item.')
      return false
    }
  }

  async function saveEdit(card: Card, text: string, assigneeName: string | null) {
    try {
      await cardStore.editCard(sprintId, card.id, { text, assigneeName })
    } catch {
      onError('Failed to update card.')
    }
  }

  function requestDelete(card: Card) {
    deleteTarget.value = card
  }

  function cancelDelete() {
    deleteTarget.value = null
  }

  async function confirmDelete() {
    const card = deleteTarget.value
    if (!card) return
    deleteTarget.value = null
    try {
      await cardStore.deleteCard(sprintId, card.id)
    } catch {
      onError('Failed to delete card.')
    }
  }

  async function castGroupVote(group: CardGroup) {
    try {
      await voteStore.castVote(sprintId, group.id)
    } catch {
      onError('Failed to cast vote.')
    }
  }

  async function retractGroupVote(group: CardGroup) {
    try {
      await voteStore.retractVote(sprintId, group.id)
    } catch {
      onError('Failed to retract vote.')
    }
  }

  async function attachEvidenceLink(cardId: number, url: string) {
    try {
      await cardStore.attachEvidenceLink(sprintId, cardId, url)
    } catch {
      onError('Failed to attach link.')
    }
  }

  async function detachEvidenceLink(cardId: number, evidenceLinkId: number) {
    try {
      await cardStore.detachEvidenceLink(sprintId, cardId, evidenceLinkId)
    } catch {
      onError('Failed to remove link.')
    }
  }

  async function focusGroup(group: CardGroup) {
    try {
      await cardGroupStore.focusGroup(sprintId, group.id)
    } catch {
      onError('Failed to focus group.')
    }
  }

  async function toggleDiscussed(group: CardGroup) {
    try {
      await cardGroupStore.toggleDiscussed(sprintId, group.id)
    } catch {
      onError('Failed to toggle discussed state.')
    }
  }

  async function dissolveGroup(group: CardGroup) {
    try {
      await cardGroupStore.dissolveGroup(sprintId, group.id)
    } catch {
      onError('Failed to dissolve group.')
    }
  }

  async function updateGroupTitle(group: CardGroup, title: string | null) {
    try {
      await cardGroupStore.updateGroupTitle(sprintId, group.id, title)
    } catch {
      onError('Failed to update group title.')
    }
  }

  async function mergeGroups(sourceGroupId: number, targetGroupId: number) {
    const source = cardGroupStore.groups.find((g) => g.id === sourceGroupId)
    const target = cardGroupStore.groups.find((g) => g.id === targetGroupId)
    if (!source || !target) return
    const [sourceSoloCard] = source.cards.length === 1 ? source.cards : []
    const [targetSoloCard] = target.cards.length === 1 ? target.cards : []
    try {
      if (sourceSoloCard && !targetSoloCard)
        await cardGroupStore.addCardToGroup(sprintId, target.id, sourceSoloCard.id)
      else if (!sourceSoloCard && targetSoloCard)
        await cardGroupStore.addCardToGroup(sprintId, source.id, targetSoloCard.id)
      else
        await cardGroupStore.createGroup(sprintId, [
          ...source.cards.map((c) => c.id),
          ...target.cards.map((c) => c.id),
        ])
    } catch {
      onError('Failed to merge groups.')
    }
  }

  async function removeCardFromGroup(groupId: number, cardId: number) {
    try {
      await cardGroupStore.removeCardFromGroup(sprintId, groupId, cardId)
    } catch {
      onError('Failed to remove card from group.')
    }
  }

  function dropOnColumn(e: DragEvent, column: ColumnType) {
    const raw = e.dataTransfer?.getData('application/json')
    if (!raw) return
    let payload: DragPayload
    try {
      payload = JSON.parse(raw)
    } catch {
      return
    }
    if (payload.type !== 'card' || payload.columnType !== column) return
    if (!payload.sourceGroupId || !payload.cardId) return
    removeCardFromGroup(payload.sourceGroupId, payload.cardId)
  }

  async function renameColumn(column: ColumnType, name: string, color: ColumnColor) {
    if (column === 'ActionItems') return
    const configOf = (c: { type: NoteColumnType; name: string; color: ColumnColor }) =>
      c.type === column ? { name, color } : { name: c.name, color: c.color }
    const [column1, column2, column3] = noteColumns.value.map(configOf)
    if (!column1 || !column2 || !column3) return
    try {
      await sprintStore.updateColumns(sprintId, {
        column1Name: column1.name,
        column1Color: column1.color,
        column2Name: column2.name,
        column2Color: column2.color,
        column3Name: column3.name,
        column3Color: column3.color,
      })
    } catch {
      onError('Failed to update column configuration.')
    }
  }

  const actions = {
    addNote,
    addActionItem,
    saveEdit,
    requestDelete,
    castGroupVote,
    retractGroupVote,
    attachEvidenceLink,
    detachEvidenceLink,
    focusGroup,
    toggleDiscussed,
    dissolveGroup,
    updateGroupTitle,
    mergeGroups,
    removeCardFromGroup,
    dropOnColumn,
    renameColumn,
  }

  return {
    // state
    participants,
    actionItems,
    noteColumns,
    showGroups,
    isScrumMaster,
    showReadiness,
    canCreateNote,
    canCreateActionItem,
    showActionItems,
    showPreviousActions,
    teamId,
    boardGridClasses,
    deleteTarget,
    // prop builders
    cardItemProps,
    groupItemProps,
    // handlers
    actions,
    cancelDelete,
    confirmDelete,
  }
}

<template>
  <div
    class="rounded-md shadow-card p-3 flex flex-col transition-all hover:shadow-card-hover"
    :class="[isSolo ? 'gap-1' : 'gap-2', groupBgClass, dragClasses]"
    :draggable="canGroup"
    @dragstart="onDragStart"
    @dragend="dragging = false"
    @dragover="onDragOver"
    @dragleave="dragOver = false"
    @drop="onDrop"
  >
    <!-- Solo group: plain card style -->
    <template v-if="isSolo">
      <div class="flex items-start gap-1">
        <GripVertical v-if="canGroup" :size="14" class="text-text-muted flex-shrink-0 mt-0.5" />
        <p class="text-sm text-text-primary whitespace-pre-wrap flex-1">{{ soloCard?.text }}</p>
        <RetroGroupVotes v-bind="votes" class="flex-shrink-0" />
      </div>
      <RetroGroupLinks v-if="soloCard?.evidenceLinks.length" :links="soloCard.evidenceLinks" />
    </template>

    <!-- Multi-card group -->
    <template v-else>
      <div class="flex items-center gap-2">
        <GripVertical v-if="canGroup" :size="14" class="text-text-muted flex-shrink-0" />
        <span class="text-sm font-medium text-text-primary flex-1 truncate">{{
          group.title || group.cards[0]?.text || 'Untitled group'
        }}</span>
        <RetroGroupVotes v-bind="votes" />
      </div>
      <RetroGroupCardList v-if="expanded" :group="group" :can-ungroup="canUngroup" />
      <div class="flex items-center gap-2">
        <button
          class="text-xs text-primary-600 hover:text-primary-800 cursor-pointer"
          @click="expanded = !expanded"
        >
          {{ expanded ? 'Collapse' : `Show ${group.cards.length} cards` }}
        </button>
        <template v-if="isScrumMaster">
          <button
            v-if="canGroup"
            class="text-xs text-text-muted hover:text-text-secondary cursor-pointer"
            @click="editingTitle = true"
          >
            Rename
          </button>
          <button
            v-if="canUngroup && group.cards.length > 1"
            class="text-xs text-danger hover:text-danger-active cursor-pointer"
            @click="actions.dissolveGroup(group)"
          >
            Dissolve
          </button>
        </template>
      </div>
    </template>

    <RetroGroupDiscussion
      v-if="isDiscussing"
      :group="group"
      :is-scrum-master="isScrumMaster"
      @focus="actions.focusGroup(group)"
      @toggle-discussed="actions.toggleDiscussed(group)"
    />
    <div
      v-if="isSolo && isScrumMaster && canGroup"
      class="flex items-center gap-2 mt-1 justify-end"
    >
      <button
        class="text-xs text-text-muted hover:text-text-secondary cursor-pointer"
        @click="editingTitle = true"
      >
        Rename
      </button>
    </div>
    <RetroGroupTitleEditor
      v-if="editingTitle"
      v-model="titleInput"
      @save="saveTitle"
      @cancel="editingTitle = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, inject } from 'vue'
import { GripVertical } from 'lucide-vue-next'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'
import type { CardGroup, DragPayload } from '@/types/card'
import type { ColumnColor } from '@/types/domain'
import RetroGroupCardList from './RetroGroupCardList.vue'
import RetroGroupDiscussion from './RetroGroupDiscussion.vue'
import RetroGroupLinks from './RetroGroupLinks.vue'
import RetroGroupTitleEditor from './RetroGroupTitleEditor.vue'
import RetroGroupVotes from './RetroGroupVotes.vue'

const props = defineProps<{
  group: CardGroup
  columnColor: ColumnColor
  showVoting: boolean
  voteCount: number
  myVoteCount: number
  remainingBudget: number
  isScrumMaster: boolean
  canGroup: boolean
  canUngroup: boolean
  isDiscussing?: boolean
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroGroupItem')

const expanded = ref(props.group.cards.length === 1)
const editingTitle = ref(false)
const titleInput = ref(props.group.title ?? '')
const dragging = ref(false)
const dragOver = ref(false)

const isSolo = computed(() => props.group.cards.length === 1)
const soloCard = computed(() => props.group.cards[0])

const votes = computed(() => ({
  group: props.group,
  showVoting: props.showVoting,
  voteCount: props.voteCount,
  myVoteCount: props.myVoteCount,
  remainingBudget: props.remainingBudget,
}))

const groupBgClass = computed(() => {
  if (props.group.isFocused) return 'bg-surface-focus ring-2 ring-primary-500'
  if (props.group.isDiscussed) return `bg-col-${props.columnColor}-light opacity-75`
  return `bg-col-${props.columnColor}-light`
})

const dragClasses = computed(() => ({
  'opacity-50': dragging.value,
  'ring-2 ring-primary-400': dragOver.value && !props.group.isFocused,
  'cursor-grab': props.canGroup && !dragging.value,
  'cursor-grabbing': dragging.value,
}))

function onDragStart(e: DragEvent) {
  if (!props.canGroup || !e.dataTransfer) return
  dragging.value = true
  const payload: DragPayload = {
    type: 'group',
    groupId: props.group.id,
    columnType: props.group.columnType,
    cardIds: props.group.cards.map((c) => c.id),
  }
  e.dataTransfer.setData('application/json', JSON.stringify(payload))
  e.dataTransfer.effectAllowed = 'move'
}

function onDragOver(e: DragEvent) {
  if (!props.canGroup) return
  try {
    if (!(e.dataTransfer?.types ?? []).includes('application/json')) return
  } catch {
    /* Firefox */
  }
  e.preventDefault()
  if (e.dataTransfer) e.dataTransfer.dropEffect = 'move'
  dragOver.value = true
}

function onDrop(e: DragEvent) {
  e.preventDefault()
  dragOver.value = false
  if (!props.canGroup) return
  const raw = e.dataTransfer?.getData('application/json')
  if (!raw) return
  let payload: DragPayload
  try {
    payload = JSON.parse(raw)
  } catch {
    return
  }
  if (payload.columnType !== props.group.columnType) return
  if (payload.groupId === props.group.id) return
  if (payload.type === 'card') {
    if (payload.sourceGroupId !== undefined && payload.cardId !== undefined)
      actions.removeCardFromGroup(payload.sourceGroupId, payload.cardId)
    return
  }
  actions.mergeGroups(payload.groupId, props.group.id)
}

function saveTitle() {
  actions.updateGroupTitle(props.group, titleInput.value.trim() || null)
  editingTitle.value = false
}
</script>

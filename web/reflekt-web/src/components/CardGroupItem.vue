<template>
  <!-- Solo group: render as plain card (no group chrome) -->
  <div
    v-if="isSolo"
    class="rounded-[--radius-md] shadow-card p-3 flex flex-col gap-1 transition-all hover:shadow-card-hover"
    :class="[groupBgClass, dragClasses]"
    :draggable="canGroup"
    @dragstart="onDragStart"
    @dragend="onDragEnd"
    @dragover="onDragOver"
    @dragleave="onDragLeave"
    @drop="onDrop"
  >
    <div class="flex gap-1">
      <GripVertical v-if="canGroup" :size="14" class="text-gray-300 flex-shrink-0 mt-0.5" />
      <p class="text-sm text-gray-800 whitespace-pre-wrap">{{ group.cards[0]?.text }}</p>
    </div>

    <!-- Evidence links for the solo card -->
    <div v-if="soloCard?.evidenceLinks.length" class="flex flex-col gap-1 mt-1">
      <a
        v-for="link in soloCard!.evidenceLinks"
        :key="link.id"
        :href="link.url"
        target="_blank"
        rel="noopener noreferrer"
        class="text-xs text-gray-600 truncate max-w-[200px] hover:text-blue-600"
      >
        {{ link.title || link.url }}
      </a>
    </div>

    <!-- Voting controls -->
    <div v-if="showVoting" class="flex items-center gap-2 mt-1">
      <button
        class="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border transition-all"
        :class="
          myVoteCount > 0
            ? 'bg-primary-600 text-white border-primary-600 hover:bg-primary-700 cursor-pointer'
            : remainingBudget <= 0
              ? 'border-gray-200 text-gray-300 cursor-not-allowed'
              : 'border-gray-300 text-gray-500 hover:border-primary-400 hover:text-primary-600 cursor-pointer'
        "
        :disabled="remainingBudget <= 0 && myVoteCount === 0"
        @click="myVoteCount > 0 ? $emit('retract-vote', group) : $emit('vote', group)"
      >
        <Check v-if="myVoteCount > 0" :size="14" />
        <ChevronUp v-else :size="14" />
        <span>{{ voteCount }}</span>
      </button>
    </div>
    <span v-else-if="voteCount > 0" class="text-xs font-medium text-gray-500">{{ voteCount }} votes</span>

    <!-- Discussion controls -->
    <div v-if="isDiscussing" class="flex items-center gap-2 mt-1">
      <CheckCircle v-if="group.isDiscussed" :size="14" class="text-green-600" />
      <span v-if="group.isDiscussed" class="text-xs text-green-600 font-medium">Discussed</span>
      <button
        v-if="isScrumMaster && !group.isFocused"
        class="text-xs text-primary-600 hover:text-primary-800 cursor-pointer flex items-center gap-1"
        @click="$emit('focus', group)"
      >
        <MessageCircle :size="12" />
        Discuss
      </button>
      <button
        v-if="isScrumMaster"
        class="text-xs text-gray-500 hover:text-gray-700 cursor-pointer"
        @click="$emit('toggle-discussed', group)"
      >
        {{ group.isDiscussed ? 'Unmark' : 'Mark discussed' }}
      </button>
    </div>

    <!-- Rename button for SM during Grouping -->
    <div v-if="isScrumMaster && canGroup" class="flex items-center gap-2 mt-1 justify-end">
      <button
        class="text-xs text-gray-500 hover:text-gray-700 cursor-pointer"
        @click="editingTitle = true"
      >
        Rename
      </button>
    </div>

    <div v-if="editingTitle" class="flex gap-1">
      <input
        v-model="titleInput"
        maxlength="100"
        placeholder="Group title..."
        class="flex-1 border border-gray-300 rounded-[--radius-sm] px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-primary-500"
        @keydown.enter="saveTitle"
        @keydown.escape="editingTitle = false"
      />
      <button
        class="px-2 py-1 text-xs bg-primary-600 text-white rounded-[--radius-sm] cursor-pointer"
        @click="saveTitle"
      >
        Save
      </button>
    </div>
  </div>

  <!-- Multi-card group: full group chrome -->
  <div
    v-else
    class="rounded-[--radius-md] shadow-card p-3 flex flex-col gap-2 transition-all hover:shadow-card-hover"
    :class="[groupBgClass, dragClasses]"
    :draggable="canGroup"
    @dragstart="onDragStart"
    @dragend="onDragEnd"
    @dragover="onDragOver"
    @dragleave="onDragLeave"
    @drop="onDrop"
  >
    <div class="flex items-center gap-2">
      <GripVertical v-if="canGroup" :size="14" class="text-gray-300 flex-shrink-0" />
      <span class="text-sm font-medium text-gray-800 flex-1 truncate">
        {{ group.title || group.cards[0]?.text || 'Untitled group' }}
      </span>
      <BaseBadge variant="info">{{ group.cards.length }}</BaseBadge>
      <div v-if="showVoting" class="flex items-center gap-1">
        <button
          class="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border transition-all"
          :class="
            myVoteCount > 0
              ? 'bg-primary-600 text-white border-primary-600 hover:bg-primary-700 cursor-pointer'
              : remainingBudget <= 0
                ? 'border-gray-200 text-gray-300 cursor-not-allowed'
                : 'border-gray-300 text-gray-500 hover:border-primary-400 hover:text-primary-600 cursor-pointer'
          "
          :disabled="remainingBudget <= 0 && myVoteCount === 0"
          @click="myVoteCount > 0 ? $emit('retract-vote', group) : $emit('vote', group)"
        >
          <Check v-if="myVoteCount > 0" :size="14" />
          <ChevronUp v-else :size="14" />
          <span>{{ voteCount }}</span>
        </button>
      </div>
      <span v-else-if="voteCount > 0" class="text-xs font-medium text-gray-500">{{ voteCount }} votes</span>
    </div>

    <div v-if="expanded" class="flex flex-col gap-1 pl-2 border-l-2 border-gray-200">
      <div
        v-for="card in group.cards"
        :key="card.id"
        class="text-xs text-gray-600 py-0.5"
        :class="{ 'cursor-grab': canUngroup }"
        :draggable="canUngroup"
        @dragstart.stop="onCardDragStart($event, card)"
      >
        {{ card.text }}
      </div>
    </div>

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
          class="text-xs text-gray-500 hover:text-gray-700 cursor-pointer"
          @click="editingTitle = true"
        >
          Rename
        </button>
        <button
          v-if="canUngroup && group.cards.length > 1"
          class="text-xs text-danger hover:text-red-700 cursor-pointer"
          @click="$emit('dissolve', group)"
        >
          Dissolve
        </button>
      </template>
    </div>

    <!-- Discussion controls -->
    <div v-if="isDiscussing" class="flex items-center gap-2">
      <CheckCircle v-if="group.isDiscussed" :size="14" class="text-green-600" />
      <span v-if="group.isDiscussed" class="text-xs text-green-600 font-medium">Discussed</span>
      <button
        v-if="isScrumMaster && !group.isFocused"
        class="text-xs text-primary-600 hover:text-primary-800 cursor-pointer flex items-center gap-1"
        @click="$emit('focus', group)"
      >
        <MessageCircle :size="12" />
        Discuss
      </button>
      <button
        v-if="isScrumMaster"
        class="text-xs text-gray-500 hover:text-gray-700 cursor-pointer"
        @click="$emit('toggle-discussed', group)"
      >
        {{ group.isDiscussed ? 'Unmark' : 'Mark discussed' }}
      </button>
    </div>

    <div v-if="editingTitle" class="flex gap-1">
      <input
        v-model="titleInput"
        maxlength="100"
        placeholder="Group title..."
        class="flex-1 border border-gray-300 rounded-[--radius-sm] px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-primary-500"
        @keydown.enter="saveTitle"
        @keydown.escape="editingTitle = false"
      />
      <button
        class="px-2 py-1 text-xs bg-primary-600 text-white rounded-[--radius-sm] cursor-pointer"
        @click="saveTitle"
      >
        Save
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronUp, Check, GripVertical, CheckCircle, MessageCircle } from 'lucide-vue-next'
import BaseBadge from './ui/BaseBadge.vue'
import type { CardGroup, CardGroupCard } from '../stores/cardGroup'

export interface DragPayload {
  type: 'group' | 'card'
  groupId: number
  columnType: string
  cardIds: number[]
  cardId?: number
  sourceGroupId?: number
}

const props = defineProps<{
  group: CardGroup
  columnColor: string
  showVoting: boolean
  voteCount: number
  myVoteCount: number
  remainingBudget: number
  isScrumMaster: boolean
  canGroup: boolean
  canUngroup: boolean
  isDiscussing?: boolean
}>()

const emit = defineEmits<{
  vote: [group: CardGroup]
  'retract-vote': [group: CardGroup]
  dissolve: [group: CardGroup]
  'update-title': [group: CardGroup, title: string | null]
  merge: [source: CardGroup, target: CardGroup]
  'remove-card': [groupId: number, cardId: number]
  focus: [group: CardGroup]
  'toggle-discussed': [group: CardGroup]
}>()

const expanded = ref(props.group.cards.length === 1)
const editingTitle = ref(false)
const titleInput = ref(props.group.title ?? '')
const dragging = ref(false)
const dragOver = ref(false)

const isSolo = computed(() => props.group.cards.length === 1)
const soloCard = computed(() => props.group.cards[0])

const groupBgClass = computed(() => {
  if (props.group.isFocused) return 'bg-primary-50 ring-2 ring-primary-500'
  if (props.group.isDiscussed) return `bg-${props.columnColor}-light/30 opacity-75`
  return `bg-${props.columnColor}-light/30`
})

const dragClasses = computed(() => ({
  'opacity-50': dragging.value,
  'ring-2 ring-primary-400': dragOver.value && !props.group.isFocused,
  'cursor-grab': props.canGroup && !dragging.value,
  'cursor-grabbing': dragging.value,
}))

function onDragStart(e: DragEvent) {
  if (!props.canGroup) return
  dragging.value = true
  const payload: DragPayload = {
    type: 'group',
    groupId: props.group.id,
    columnType: props.group.columnType,
    cardIds: props.group.cards.map((c) => c.id),
  }
  e.dataTransfer!.setData('application/json', JSON.stringify(payload))
  e.dataTransfer!.effectAllowed = 'move'
}

function onDragEnd() {
  dragging.value = false
}

function onDragOver(e: DragEvent) {
  if (!props.canGroup) return
  try {
    const types = e.dataTransfer?.types ?? []
    if (!types.includes('application/json')) return
  } catch {
    // Firefox security: can't read data during dragover, allow drop
  }
  e.preventDefault()
  e.dataTransfer!.dropEffect = 'move'
  dragOver.value = true
}

function onDragLeave() {
  dragOver.value = false
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
    emit('remove-card', payload.sourceGroupId!, payload.cardId!)
    return
  }

  emit('merge', { id: payload.groupId, columnType: payload.columnType, cards: payload.cardIds.map((id) => ({ id })) } as unknown as CardGroup, props.group)
}

function onCardDragStart(e: DragEvent, card: CardGroupCard) {
  if (!props.canUngroup) return
  const payload: DragPayload = {
    type: 'card',
    groupId: props.group.id,
    columnType: props.group.columnType,
    cardIds: [card.id],
    cardId: card.id,
    sourceGroupId: props.group.id,
  }
  e.dataTransfer!.setData('application/json', JSON.stringify(payload))
  e.dataTransfer!.effectAllowed = 'move'
}

function saveTitle() {
  const title = titleInput.value.trim() || null
  emit('update-title', props.group, title)
  editingTitle.value = false
}
</script>

<template>
  <div class="flex flex-col gap-1 pl-2 border-l-2 border-border">
    <div
      v-for="card in group.cards"
      :key="card.id"
      class="text-xs text-text-secondary py-0.5"
      :class="{ 'cursor-grab': canUngroup }"
      :draggable="canUngroup"
      @dragstart.stop="onCardDragStart($event, card)"
    >
      {{ card.text }}
    </div>
  </div>
</template>

<script setup lang="ts">
import type { CardGroup, CardGroupCard, DragPayload } from '@/types/card'

const props = defineProps<{
  group: CardGroup
  canUngroup: boolean
}>()

function onCardDragStart(e: DragEvent, card: CardGroupCard) {
  if (!props.canUngroup || !e.dataTransfer) return
  const payload: DragPayload = {
    type: 'card',
    groupId: props.group.id,
    columnType: props.group.columnType,
    cardIds: [card.id],
    cardId: card.id,
    sourceGroupId: props.group.id,
  }
  e.dataTransfer.setData('application/json', JSON.stringify(payload))
  e.dataTransfer.effectAllowed = 'move'
}
</script>

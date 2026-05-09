<template>
  <div
    v-show="visible"
    class="bg-surface border border-gray-200 rounded-[--radius-md] shadow-card flex flex-col min-h-0"
    :class="extraClass"
  >
    <!-- Column header -->
    <div
      class="px-4 py-3 border-b rounded-t-[--radius-md] flex items-center gap-2"
      :class="`bg-${columnColor}-light`"
    >
      <component :is="headerIcon" :size="18" :class="`text-${columnColor}`" />
      <h2 class="font-semibold text-base flex-1" :class="`text-${columnColor}`">{{ title }}</h2>
      <BaseBadge :variant="badgeVariant">{{ count }}</BaseBadge>
    </div>

    <!-- Card/group list -->
    <TransitionGroup
      name="card-list"
      tag="div"
      class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto min-h-0 relative"
      :class="{ 'bg-primary-50/30': dragOver }"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <slot name="empty" />
      <slot />
    </TransitionGroup>

    <!-- Add input -->
    <div v-if="$slots.input" class="p-3 border-t">
      <slot name="input" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { Component } from 'vue'
import BaseBadge from '../ui/BaseBadge.vue'

const props = defineProps<{
  title: string
  columnColor: string
  headerIcon: Component
  badgeVariant: 'success' | 'danger' | 'info' | 'primary'
  count: number
  visible: boolean
  extraClass?: string
}>()

const emit = defineEmits<{
  drop: [e: DragEvent]
}>()

const dragOver = ref(false)

function onDragOver(e: DragEvent) {
  try {
    const types = e.dataTransfer?.types ?? []
    if (!types.includes('application/json')) return
  } catch {
    // Firefox: allow drop
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
  emit('drop', e)
}
</script>

<style scoped>
.card-list-enter-active,
.card-list-leave-active {
  transition: all 0.2s ease;
}
.card-list-enter-from,
.card-list-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>

<template>
  <RetroColumnPanel
    :title="title"
    :column-color="columnColor"
    :header-icon="headerIcon"
    :badge-variant="badgeVariant"
    :count="count"
    :visible="visible"
    :extra-class="extraClass"
    @drop="emit('drop', $event)"
  >
    <template #empty>
      <slot name="empty" />
    </template>

    <slot />

    <template #input>
      <slot name="input" />
    </template>
  </RetroColumnPanel>
</template>

<script setup lang="ts">
import type { Component } from 'vue'
import RetroColumnPanel from './RetroColumnPanel.vue'

defineProps<{
  title: string
  columnColor: string
  headerIcon: Component
  badgeVariant: 'success' | 'danger' | 'info' | 'primary'
  count: number
  visible: boolean
  extraClass?: string
}>()

const emit = defineEmits<{
  drop: [event: DragEvent]
}>()
</script>

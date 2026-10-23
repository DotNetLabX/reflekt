<template>
  <div class="flex sm:hidden gap-1" role="tablist" aria-label="Board columns">
    <button
      v-for="(tab, index) in tabs"
      :id="columnTabId(tab.type)"
      :key="tab.type"
      type="button"
      role="tab"
      :aria-selected="model === tab.type"
      :aria-controls="columnPanelId(tab.type)"
      :tabindex="model === tab.type ? 0 : -1"
      class="flex-1 px-3 py-2 text-sm font-medium rounded-sm transition-colors cursor-pointer"
      :class="model === tab.type ? tab.activeClass : tab.idleClass"
      @click="model = tab.type"
      @keydown="onKeydown($event, index)"
    >
      {{ tab.label }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { columnPanelId, columnTabId } from '@/constants/columns'
import type { ColumnColor, ColumnType } from '@/types/domain'

const model = defineModel<ColumnType>({ required: true })

const props = defineProps<{
  showActionItems: boolean
  column1Name: string
  column1Color: ColumnColor
  column2Name: string
  column2Color: ColumnColor
  column3Name: string
  column3Color: ColumnColor
}>()

interface Tab {
  type: ColumnType
  label: string
  activeClass: string
  idleClass: string
}

function noteTab(type: ColumnType, label: string, color: ColumnColor): Tab {
  return {
    type,
    label,
    activeClass: `bg-col-${color} text-white`,
    idleClass: `bg-col-${color}-light text-col-${color}`,
  }
}

const tabs = computed<Tab[]>(() => {
  const list = [
    noteTab('Column1', props.column1Name, props.column1Color),
    noteTab('Column2', props.column2Name, props.column2Color),
    noteTab('Column3', props.column3Name, props.column3Color),
  ]
  if (props.showActionItems) {
    list.push({
      type: 'ActionItems',
      label: 'Actions',
      activeClass: 'bg-action-items text-white',
      idleClass: 'bg-action-items-light text-action-items',
    })
  }
  return list
})

function onKeydown(event: KeyboardEvent, index: number) {
  const last = tabs.value.length - 1
  const targets: Record<string, number> = {
    ArrowRight: index === last ? 0 : index + 1,
    ArrowLeft: index === 0 ? last : index - 1,
    Home: 0,
    End: last,
  }
  const tab = tabs.value[targets[event.key] ?? -1]
  if (!tab) return
  event.preventDefault()
  model.value = tab.type
  document.getElementById(columnTabId(tab.type))?.focus()
}
</script>

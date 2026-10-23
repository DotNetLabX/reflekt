<template>
  <section
    :id="columnPanelId(column)"
    role="tabpanel"
    :aria-labelledby="columnTabId(column)"
    class="bg-surface border border-border rounded-md shadow-card flex-col min-h-0"
    :class="mobileHidden ? 'hidden sm:flex' : 'flex'"
  >
    <!-- Column header -->
    <div
      class="px-4 py-3 border-b rounded-t-md flex items-center gap-2"
      :class="`bg-col-${columnColor}-light`"
    >
      <!-- Inline edit mode (SM only); focus leaving the name and swatches confirms the edit -->
      <div
        v-if="editing"
        ref="editor"
        class="flex-1 flex items-center gap-2"
        @focusout="onEditorFocusOut"
      >
        <input
          ref="nameInput"
          v-model="editName"
          type="text"
          maxlength="30"
          aria-label="Column name"
          class="flex-1 text-sm font-semibold bg-surface border border-border rounded px-2 py-0.5 focus:outline-none focus:ring-2 focus:ring-primary-500"
          :class="`text-col-${columnColor}`"
          @keydown.enter="confirmEdit"
        />
        <div class="flex gap-1">
          <button
            v-for="color in PALETTE_COLORS"
            :key="color"
            type="button"
            :title="color"
            :aria-label="color"
            :aria-pressed="editColor === color"
            class="w-4 h-4 rounded-full border-2 transition-transform cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
            :class="[
              `bg-col-${color}`,
              editColor === color ? 'border-text-primary scale-110' : 'border-transparent',
            ]"
            @mousedown.prevent
            @click="selectColor(color)"
          />
        </div>
      </div>

      <!-- Display mode -->
      <template v-else>
        <h2 class="font-semibold text-base flex-1" :class="`text-col-${columnColor}`">
          <button
            v-if="isScrumMaster"
            type="button"
            class="text-left cursor-pointer hover:underline"
            :aria-label="`Rename column ${title}`"
            @click="startEdit"
          >
            {{ title }}
          </button>
          <template v-else>{{ title }}</template>
        </h2>
      </template>

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
  </section>
</template>

<script setup lang="ts">
import { ref, nextTick, inject } from 'vue'
import BaseBadge from '@/components/ui/BaseBadge.vue'
import { PALETTE_COLORS, columnPanelId, columnTabId } from '@/constants/columns'
import { boardActionsKey, missingBoardActions } from '@/composables/useRetroBoardActions'
import type { ColumnColor, ColumnType } from '@/types/domain'

const props = defineProps<{
  column: ColumnType
  title: string
  columnColor: ColumnColor
  badgeVariant: 'success' | 'danger' | 'info' | 'primary'
  count: number
  mobileHidden: boolean
  isScrumMaster: boolean
}>()

const actions = inject(boardActionsKey) ?? missingBoardActions('RetroColumnPanel')

const dragOver = ref(false)
const editing = ref(false)
const editName = ref('')
const editColor = ref<ColumnColor>(props.columnColor)
const nameInput = ref<HTMLInputElement | null>(null)
const editor = ref<HTMLElement | null>(null)

function startEdit() {
  editName.value = props.title
  editColor.value = props.columnColor
  editing.value = true
  nextTick(() => nameInput.value?.focus())
}

function confirmEdit() {
  if (!editing.value) return
  editing.value = false
  const name = editName.value.trim()
  if (!name || (name === props.title && editColor.value === props.columnColor)) return
  actions.renameColumn(props.column, name, editColor.value)
}

function selectColor(color: ColumnColor) {
  editColor.value = color
}

function onEditorFocusOut(e: FocusEvent) {
  if (e.relatedTarget instanceof Node && editor.value?.contains(e.relatedTarget)) return
  confirmEdit()
}

function onDragOver(e: DragEvent) {
  try {
    const types = e.dataTransfer?.types ?? []
    if (!types.includes('application/json')) return
  } catch {
    // Firefox: allow drop
  }
  e.preventDefault()
  if (e.dataTransfer) e.dataTransfer.dropEffect = 'move'
  dragOver.value = true
}

function onDragLeave() {
  dragOver.value = false
}

function onDrop(e: DragEvent) {
  e.preventDefault()
  dragOver.value = false
  actions.dropOnColumn(e, props.column)
}
</script>

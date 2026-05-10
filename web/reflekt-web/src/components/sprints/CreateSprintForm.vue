<template>
  <div class="flex flex-col gap-2 pt-2 border-t border-gray-100">
    <h3 class="text-sm font-semibold text-gray-700">New Sprint</h3>
    <input
      v-model="sprintName"
      type="text"
      placeholder="Sprint name"
      maxlength="100"
      class="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
    />
    <input
      v-model.number="votesPerParticipant"
      type="number"
      placeholder="Votes per participant (default 3)"
      min="1"
      class="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
    />

    <!-- Column configuration rows -->
    <div class="flex flex-col gap-2 mt-1">
      <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Column Configuration</p>
      <!-- Column 1 -->
      <div class="flex items-center gap-2">
        <input v-model="col1.name" type="text" placeholder="Column 1 name" maxlength="30"
          class="flex-1 border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col1.color === color ? 'border-gray-800 scale-110' : 'border-transparent']"
            @click="col1.color = color" />
        </div>
      </div>
      <!-- Column 2 -->
      <div class="flex items-center gap-2">
        <input v-model="col2.name" type="text" placeholder="Column 2 name" maxlength="30"
          class="flex-1 border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col2.color === color ? 'border-gray-800 scale-110' : 'border-transparent']"
            @click="col2.color = color" />
        </div>
      </div>
      <!-- Column 3 -->
      <div class="flex items-center gap-2">
        <input v-model="col3.name" type="text" placeholder="Column 3 name" maxlength="30"
          class="flex-1 border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col3.color === color ? 'border-gray-800 scale-110' : 'border-transparent']"
            @click="col3.color = color" />
        </div>
      </div>
    </div>

    <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    <button
      class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
      :disabled="busy"
      @click="handleCreate"
    >
      {{ busy ? 'Creating…' : 'Create Sprint' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSprintStore } from '../../stores/sprint'

const PALETTE_COLORS = ['green', 'red', 'amber', 'blue', 'purple', 'teal', 'pink', 'slate'] as const
type PaletteColor = typeof PALETTE_COLORS[number]

const paletteColors = PALETTE_COLORS

const DEFAULT_COLUMNS = [
  { name: 'Went Well', color: 'green' as PaletteColor },
  { name: "Didn't Go Well", color: 'red' as PaletteColor },
  { name: 'Start Doing', color: 'amber' as PaletteColor },
]

const emit = defineEmits<{
  create: [payload: {
    name: string
    votesPerParticipant: number | undefined
    column1Name: string
    column1Color: string
    column2Name: string
    column2Color: string
    column3Name: string
    column3Color: string
  }]
}>()

const sprintStore = useSprintStore()
const sprintName = ref('')
const votesPerParticipant = ref<number | undefined>(undefined)
const error = ref('')
const busy = ref(false)

const col1 = ref({ name: DEFAULT_COLUMNS[0]!.name, color: DEFAULT_COLUMNS[0]!.color as string })
const col2 = ref({ name: DEFAULT_COLUMNS[1]!.name, color: DEFAULT_COLUMNS[1]!.color as string })
const col3 = ref({ name: DEFAULT_COLUMNS[2]!.name, color: DEFAULT_COLUMNS[2]!.color as string })

onMounted(() => {
  // Pre-fill from most recent sprint if available
  const mostRecent = sprintStore.sprints[0]
  if (mostRecent) {
    col1.value.name = mostRecent.column1Name
    col1.value.color = mostRecent.column1Color
    col2.value.name = mostRecent.column2Name
    col2.value.color = mostRecent.column2Color
    col3.value.name = mostRecent.column3Name
    col3.value.color = mostRecent.column3Color
  }
})

async function handleCreate() {
  error.value = ''
  const name = sprintName.value.trim()
  if (!name) { error.value = 'Sprint name is required.'; return }
  if (!col1.value.name.trim()) { error.value = 'Column 1 name is required.'; return }
  if (!col2.value.name.trim()) { error.value = 'Column 2 name is required.'; return }
  if (!col3.value.name.trim()) { error.value = 'Column 3 name is required.'; return }
  busy.value = true
  try {
    emit('create', {
      name,
      votesPerParticipant: votesPerParticipant.value,
      column1Name: col1.value.name.trim(),
      column1Color: col1.value.color,
      column2Name: col2.value.name.trim(),
      column2Color: col2.value.color,
      column3Name: col3.value.name.trim(),
      column3Color: col3.value.color,
    })
    sprintName.value = ''
    votesPerParticipant.value = undefined
  } finally {
    busy.value = false
  }
}
</script>

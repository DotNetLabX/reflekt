<template>
  <div class="flex flex-col gap-2 pt-2 border-t border-border-subtle">
    <h3 class="text-sm font-semibold text-text-secondary">New Sprint</h3>
    <BaseInput
      v-model="sprintName"
      placeholder="Sprint name"
      aria-label="Sprint name"
      :maxlength="100"
    />
    <BaseInput
      v-model.number="votesPerParticipant"
      type="number"
      placeholder="Votes per participant (default 3)"
      aria-label="Votes per participant (default 3)"
      min="1"
    />

    <!-- Column configuration rows -->
    <div class="flex flex-col gap-2 mt-1">
      <p class="text-xs font-medium text-text-muted uppercase tracking-wide">
        Column Configuration
      </p>
      <SprintColumnConfigRow
        v-for="(column, i) in [col1, col2, col3]"
        :key="i"
        v-model:name="column.name"
        v-model:color="column.color"
        :index="i + 1"
        @edited="(field) => edited.add(`col${i + 1}.${field}`)"
      />
    </div>

    <!-- Timer defaults -->
    <div class="flex flex-col gap-2 mt-1">
      <p class="text-xs font-medium text-text-muted uppercase tracking-wide">
        Timer Defaults (minutes)
      </p>
      <div class="flex gap-2">
        <div v-for="field in timerFields" :key="field.key" class="flex flex-col gap-1 flex-1">
          <label :for="timerInputId(field.key)" class="text-xs text-text-secondary">{{
            field.label
          }}</label>
          <BaseInput
            :id="timerInputId(field.key)"
            v-model.number="timerDefaults[field.key].value"
            type="number"
            min="1"
            max="60"
          />
        </div>
      </div>
    </div>

    <p v-if="error" class="text-danger text-sm">{{ error }}</p>
    <BaseButton :disabled="busy" @click="handleCreate">
      {{ busy ? 'Creating…' : 'Create Sprint' }}
    </BaseButton>
  </div>
</template>

<script setup lang="ts">
import { ref, useId, watch, type Ref } from 'vue'
import { describeApiError } from '@/api/client'
import { useSprintStore } from '@/stores/sprint'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseInput from '@/components/ui/BaseInput.vue'
import { DEFAULT_COLUMNS, DEFAULT_TIMERS } from '@/constants/columns'
import { PHASES, type ColumnColor } from '@/types/domain'
import SprintColumnConfigRow from './SprintColumnConfigRow.vue'

const props = defineProps<{ teamId: number }>()

const sprintStore = useSprintStore()
const sprintName = ref('')
const votesPerParticipant = ref<number | undefined>(undefined)
const error = ref('')
const busy = ref(false)
const writingTimerDefault = ref(DEFAULT_TIMERS.writingTimerDefault)
const votingTimerDefault = ref(DEFAULT_TIMERS.votingTimerDefault)
const discussingTimerDefault = ref(DEFAULT_TIMERS.discussingTimerDefault)
const timerDefaults = { writingTimerDefault, votingTimerDefault, discussingTimerDefault }
const timerFields = PHASES.flatMap((phase) =>
  phase.timerDefault ? [{ key: phase.timerDefault, label: phase.name }] : [],
)
const formId = useId()

function timerInputId(field: keyof typeof timerDefaults): string {
  return `${formId}-${field}`
}

const col1 = ref({ ...DEFAULT_COLUMNS[0] })
const col2 = ref({ ...DEFAULT_COLUMNS[1] })
const col3 = ref({ ...DEFAULT_COLUMNS[2] })

// The list is cleared when its load starts, so the pre-fill from the most recent
// sprint waits for that load to end and is applied once. The form is editable
// meanwhile, so a field the user already changed keeps the user's value.
const edited = new Set<string>()
let prefilled = false

function prefillColumn(
  column: Ref<{ name: string; color: ColumnColor }>,
  key: string,
  name: string,
  color: ColumnColor,
) {
  if (!edited.has(`${key}.name`)) column.value.name = name
  if (!edited.has(`${key}.color`)) column.value.color = color
}

watch(
  () => sprintStore.sprintsLoading,
  (loading) => {
    const mostRecent = sprintStore.sprints[0]
    if (loading || prefilled || !mostRecent) return
    prefilled = true
    prefillColumn(col1, 'col1', mostRecent.column1Name, mostRecent.column1Color)
    prefillColumn(col2, 'col2', mostRecent.column2Name, mostRecent.column2Color)
    prefillColumn(col3, 'col3', mostRecent.column3Name, mostRecent.column3Color)
  },
  { immediate: true },
)

function validateTimerDefault(value: number, label: string): boolean {
  if (!Number.isInteger(value) || value < 1 || value > 60) {
    error.value = `${label} timer must be between 1 and 60 minutes.`
    return false
  }
  return true
}

async function handleCreate() {
  error.value = ''
  const name = sprintName.value.trim()
  if (!name) {
    error.value = 'Sprint name is required.'
    return
  }
  if (!col1.value.name.trim()) {
    error.value = 'Column 1 name is required.'
    return
  }
  if (!col2.value.name.trim()) {
    error.value = 'Column 2 name is required.'
    return
  }
  if (!col3.value.name.trim()) {
    error.value = 'Column 3 name is required.'
    return
  }
  for (const phase of PHASES) {
    if (
      phase.timerDefault &&
      !validateTimerDefault(timerDefaults[phase.timerDefault].value, phase.name)
    )
      return
  }
  busy.value = true
  try {
    await sprintStore.createSprint(props.teamId, {
      name,
      votesPerParticipant: votesPerParticipant.value,
      column1Name: col1.value.name.trim(),
      column1Color: col1.value.color,
      column2Name: col2.value.name.trim(),
      column2Color: col2.value.color,
      column3Name: col3.value.name.trim(),
      column3Color: col3.value.color,
      writingTimerDefault: writingTimerDefault.value,
      votingTimerDefault: votingTimerDefault.value,
      discussingTimerDefault: discussingTimerDefault.value,
    })
    sprintName.value = ''
    votesPerParticipant.value = undefined
  } catch (err: unknown) {
    error.value = describeApiError(err, { fallback: 'Failed to create sprint.' })
  } finally {
    busy.value = false
  }
}
</script>

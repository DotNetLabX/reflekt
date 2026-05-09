<template>
  <div class="flex flex-col gap-4 p-8 bg-white rounded shadow w-full max-w-md">
    <h2 class="text-xl font-bold text-gray-800">{{ teamName }}</h2>

    <div v-if="sprints.length === 0" class="text-gray-400 text-sm">
      No sprints yet. Create your first sprint.
    </div>
    <ul v-else class="flex flex-col gap-2">
      <li
        v-for="sprint in sprints"
        :key="sprint.id"
        class="flex items-center justify-between px-3 py-2 border border-gray-200 rounded hover:bg-gray-50 cursor-pointer"
        @click="$emit('select', sprint.id)"
      >
        <span class="text-sm font-medium text-gray-800">{{ sprint.name }}</span>
        <div class="flex items-center gap-2 text-xs">
          <span class="text-gray-500">{{ sprint.currentPhase }}</span>
          <span :class="stateBadge(sprint.state)">{{ sprint.state }}</span>
        </div>
      </li>
    </ul>

    <CreateSprintForm @create="$emit('create-sprint', $event)" />
  </div>
</template>

<script setup lang="ts">
import CreateSprintForm from './CreateSprintForm.vue'

defineProps<{
  teamName: string
  sprints: { id: number; name: string; currentPhase: string; state: string }[]
}>()

defineEmits<{
  select: [sprintId: number]
  'create-sprint': [payload: { name: string; votesPerParticipant: number | undefined }]
}>()

function stateBadge(state: string) {
  const base = 'px-2 py-0.5 rounded font-semibold'
  if (state === 'Draft') return `${base} bg-gray-100 text-gray-600`
  if (state === 'Active') return `${base} bg-green-100 text-green-700`
  return `${base} bg-red-100 text-red-700`
}
</script>

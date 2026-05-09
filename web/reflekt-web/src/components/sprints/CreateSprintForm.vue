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
import { ref } from 'vue'

const emit = defineEmits<{
  create: [payload: { name: string; votesPerParticipant: number | undefined }]
}>()

const sprintName = ref('')
const votesPerParticipant = ref<number | undefined>(undefined)
const error = ref('')
const busy = ref(false)

async function handleCreate() {
  error.value = ''
  const name = sprintName.value.trim()
  if (!name) { error.value = 'Sprint name is required.'; return }
  busy.value = true
  try {
    emit('create', { name, votesPerParticipant: votesPerParticipant.value })
    sprintName.value = ''
    votesPerParticipant.value = undefined
  } finally {
    busy.value = false
  }
}
</script>

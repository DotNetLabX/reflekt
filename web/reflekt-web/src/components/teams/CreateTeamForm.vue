<template>
  <div class="flex flex-col gap-3 p-8 bg-white rounded shadow w-72">
    <h2 class="text-lg font-semibold text-gray-800">Create your first team</h2>
    <input
      v-model="teamName"
      type="text"
      placeholder="Team name"
      maxlength="100"
      class="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
    />
    <p v-if="errorMessage" class="text-red-600 text-sm">{{ errorMessage }}</p>
    <button
      class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
      :disabled="busy"
      @click="handleCreate"
    >
      {{ busy ? 'Creating…' : 'Create Team' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  create: [name: string]
}>()

const teamName = ref('')
const errorMessage = ref('')
const busy = ref(false)

async function handleCreate() {
  errorMessage.value = ''
  const name = teamName.value.trim()
  if (!name) { errorMessage.value = 'Team name is required.'; return }
  busy.value = true
  try {
    emit('create', name)
    teamName.value = ''
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-50">
    <!-- Name picker -->
    <div v-if="!joined" class="w-full max-w-sm p-8 bg-white rounded shadow flex flex-col gap-4">
      <h1 class="text-2xl font-bold text-gray-800">Join Retro</h1>
      <p class="text-sm text-gray-500">Sprint #{{ sprintId }}</p>

      <p v-if="errorMessage" class="text-sm text-red-600">{{ errorMessage }}</p>

      <form class="flex flex-col gap-3" @submit.prevent="handleJoin">
        <input
          v-model="displayName"
          type="text"
          maxlength="50"
          placeholder="Your display name"
          class="border rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          :disabled="busy"
          required
        />
        <button
          type="submit"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
          :disabled="busy || !displayName.trim()"
        >
          Join
        </button>
      </form>
    </div>

    <!-- Retro room shell -->
    <div v-else class="w-full max-w-2xl p-8 bg-white rounded shadow flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-gray-800">Sprint #{{ sprintId }}</h1>
        <span class="text-sm text-gray-500">Retro Room</span>
      </div>

      <div class="flex flex-col gap-2">
        <h2 class="text-sm font-semibold text-gray-600 uppercase tracking-wide">Participants</h2>
        <div class="flex flex-wrap gap-2">
          <span
            v-for="p in participants"
            :key="p.id"
            class="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm"
          >
            {{ p.displayName }}
          </span>
        </div>
      </div>

      <div class="border-t pt-4 text-center text-gray-400 text-sm">
        Cards and voting coming soon…
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useSprintHub } from '../composables/useSprintHub'

const route = useRoute()
const guestStore = useGuestStore()
const participantStore = useParticipantStore()
const { connect, disconnect } = useSprintHub()

const sprintId = Number(route.params.sprintId)
const displayName = ref('')
const busy = ref(false)
const errorMessage = ref('')
const joined = ref(false)

const participants = computed(() => participantStore.participants)

onMounted(async () => {
  guestStore.loadToken(sprintId)
  if (guestStore.token) {
    try {
      await participantStore.fetchMe(sprintId)
      await enterRoom()
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status
      if (status === 401) {
        guestStore.clearToken(sprintId)
      }
    }
  }
})

onUnmounted(() => disconnect())

async function enterRoom() {
  await participantStore.fetchParticipants(sprintId)
  await connect(sprintId)
  joined.value = true
}

async function handleJoin() {
  if (!displayName.value.trim()) return
  busy.value = true
  errorMessage.value = ''
  try {
    const response = await participantStore.join(sprintId, displayName.value.trim())
    guestStore.saveToken(sprintId, response.guestToken, response.displayName)
    await enterRoom()
  } catch (err: unknown) {
    const status = (err as { status?: number })?.status
    if (status === 409) errorMessage.value = 'Sprint is not accepting participants.'
    else if (status === 404) errorMessage.value = 'Sprint not found.'
    else errorMessage.value = 'Failed to join. Please try again.'
  } finally {
    busy.value = false
  }
}
</script>

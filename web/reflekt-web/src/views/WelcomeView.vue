<template>
  <div class="flex items-center justify-center min-h-screen">
    <!-- Not signed in -->
    <div v-if="!authStore.isSignedIn" class="flex flex-col items-center gap-4">
      <h1 class="text-2xl font-semibold text-gray-800">Welcome to Reflekt</h1>
      <button
        class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        @click="authStore.signIn()"
      >
        Sign in with Google
      </button>
    </div>

    <!-- Signed in, has team -->
    <div v-else-if="teamsStore.hasTeam" class="flex flex-col items-center gap-2 p-8 bg-white rounded shadow">
      <h2 class="text-xl font-bold text-gray-800">{{ teamsStore.currentTeam!.name }}</h2>
      <p class="text-gray-500 text-sm">Sprint management coming next.</p>
    </div>

    <!-- Signed in, no team -->
    <div v-else class="flex flex-col gap-3 p-8 bg-white rounded shadow w-72">
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
        :disabled="creating"
        @click="handleCreateTeam"
      >
        {{ creating ? 'Creating…' : 'Create Team' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useTeamsStore } from '../stores/teams';

const authStore = useAuthStore();
const teamsStore = useTeamsStore();

const teamName = ref('');
const errorMessage = ref('');
const creating = ref(false);

watch(
  () => authStore.loaded,
  (loaded) => {
    if (loaded && authStore.isSignedIn) {
      teamsStore.fetchMyTeams();
    }
  },
  { immediate: true },
);

async function handleCreateTeam() {
  errorMessage.value = '';
  const name = teamName.value.trim();
  if (!name) {
    errorMessage.value = 'Team name is required.';
    return;
  }
  creating.value = true;
  try {
    await teamsStore.createTeam(name);
    teamName.value = '';
  } catch {
    errorMessage.value = 'Failed to create team. Please try again.';
  } finally {
    creating.value = false;
  }
}
</script>

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
    <div v-else-if="teamsStore.hasTeam" class="flex flex-col gap-4 p-8 bg-white rounded shadow w-full max-w-md">
      <h2 class="text-xl font-bold text-gray-800">{{ teamsStore.currentTeam!.name }}</h2>

      <!-- Sprint list -->
      <div v-if="sprintStore.sprints.length === 0" class="text-gray-400 text-sm">
        No sprints yet. Create your first sprint.
      </div>
      <ul v-else class="flex flex-col gap-2">
        <li
          v-for="sprint in sprintStore.sprints"
          :key="sprint.id"
          class="flex items-center justify-between px-3 py-2 border border-gray-200 rounded hover:bg-gray-50 cursor-pointer"
          @click="router.push({ name: 'sprint', params: { teamId: teamsStore.currentTeam!.id, sprintId: sprint.id } })"
        >
          <span class="text-sm font-medium text-gray-800">{{ sprint.name }}</span>
          <div class="flex items-center gap-2 text-xs">
            <span class="text-gray-500">{{ sprint.currentPhase }}</span>
            <span :class="stateBadge(sprint.state)">{{ sprint.state }}</span>
          </div>
        </li>
      </ul>

      <!-- Create sprint form -->
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
        <p v-if="sprintError" class="text-red-600 text-sm">{{ sprintError }}</p>
        <button
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50"
          :disabled="creatingSprint"
          @click="handleCreateSprint"
        >
          {{ creatingSprint ? 'Creating…' : 'Create Sprint' }}
        </button>
      </div>
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
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useTeamsStore } from '../stores/teams';
import { useSprintStore } from '../stores/sprint';

const router = useRouter();
const authStore = useAuthStore();
const teamsStore = useTeamsStore();
const sprintStore = useSprintStore();

const teamName = ref('');
const errorMessage = ref('');
const creating = ref(false);

const sprintName = ref('');
const votesPerParticipant = ref<number | undefined>(undefined);
const sprintError = ref('');
const creatingSprint = ref(false);

watch(
  () => authStore.loaded,
  (loaded) => {
    if (loaded && authStore.isSignedIn) {
      teamsStore.fetchMyTeams();
    }
  },
  { immediate: true },
);

watch(
  () => teamsStore.currentTeam,
  (team) => {
    if (team) sprintStore.fetchTeamSprints(team.id);
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

async function handleCreateSprint() {
  sprintError.value = '';
  const name = sprintName.value.trim();
  if (!name) { sprintError.value = 'Sprint name is required.'; return; }
  creatingSprint.value = true;
  try {
    await sprintStore.createSprint(teamsStore.currentTeam!.id, name, votesPerParticipant.value);
    sprintName.value = '';
    votesPerParticipant.value = undefined;
  } catch {
    sprintError.value = 'Failed to create sprint. Please try again.';
  } finally {
    creatingSprint.value = false;
  }
}

function stateBadge(state: string) {
  const base = 'px-2 py-0.5 rounded font-semibold';
  if (state === 'Draft') return `${base} bg-gray-100 text-gray-600`;
  if (state === 'Active') return `${base} bg-green-100 text-green-700`;
  return `${base} bg-red-100 text-red-700`;
}
</script>

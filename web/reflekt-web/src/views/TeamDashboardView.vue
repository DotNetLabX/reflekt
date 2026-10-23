<template>
  <div class="flex flex-col items-center min-h-[calc(100vh-3.5rem)] p-8">
    <!-- Signed in -->
    <template v-if="authStore.isSignedIn">
      <EmptyState v-if="teamsStore.error" :title="teamsStore.error" class="mt-16">
        <BaseButton variant="secondary" size="sm" @click="teamsStore.fetchMyTeams()"
          >Retry</BaseButton
        >
      </EmptyState>

      <!-- Empty state: no teams -->
      <div
        v-else-if="teamsStore.loaded && !teamsStore.loading && !teamsStore.hasTeams"
        class="mt-16"
      >
        <CreateTeamForm heading="Create your first team" @created="handleTeamCreated" />
      </div>

      <!-- Teams card grid -->
      <div v-else class="w-full max-w-4xl">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-2xl font-semibold text-text-primary">Your Teams</h1>
          <BaseTooltip
            v-slot="{ describedby }"
            :text="
              teamsStore.isAtLimit
                ? 'You\'ve reached the maximum of 10 teams. Delete a team to create a new one.'
                : 'Create a new team. You can manage up to 10 teams.'
            "
            :panel-class="teamsStore.isAtLimit ? 'right-0 mt-1 w-64' : 'right-0 mt-1 w-52'"
          >
            <span
              v-if="teamsStore.isAtLimit"
              tabindex="0"
              :aria-describedby="describedby"
              class="inline-flex rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary-500 ring-offset-focus-offset"
            >
              <BaseButton disabled>Create Team</BaseButton>
            </span>
            <BaseButton v-else :aria-describedby="describedby" @click="showCreateForm = true">
              Create Team
            </BaseButton>
          </BaseTooltip>
        </div>

        <div v-if="showCreateForm" class="mb-6">
          <CreateTeamForm @created="handleTeamCreated" @cancel="showCreateForm = false" />
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <TeamCard
            v-for="team in teamsStore.teams"
            :key="team.id"
            :team="team"
            @click="navigateToTeam(team.id)"
            @delete="handleDeleteRequest(team.id)"
          />
        </div>
      </div>
    </template>

    <DeleteTeamDialog
      v-if="pendingDeleteTeam"
      :team="pendingDeleteTeam"
      :open="!!pendingDeleteTeam"
      @deleted="pendingDeleteTeam = null"
      @close="pendingDeleteTeam = null"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTeamsStore } from '@/stores/teams'
import type { TeamSummary } from '@/types/team'
import CreateTeamForm from '@/components/teams/CreateTeamForm.vue'
import TeamCard from '@/components/teams/TeamCard.vue'
import DeleteTeamDialog from '@/components/teams/DeleteTeamDialog.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseTooltip from '@/components/ui/BaseTooltip.vue'

const router = useRouter()
const authStore = useAuthStore()
const teamsStore = useTeamsStore()

const showCreateForm = ref(false)
const pendingDeleteTeam = ref<TeamSummary | null>(null)

watch(
  () => authStore.loaded,
  (loaded) => {
    if (loaded && authStore.isSignedIn) teamsStore.fetchMyTeams()
  },
  { immediate: true },
)

function navigateToTeam(teamId: number) {
  router.push({ name: 'team-sprints', params: { teamId } })
}

function handleTeamCreated(team: TeamSummary) {
  showCreateForm.value = false
  router.push({ name: 'team-sprints', params: { teamId: team.id } })
}

function handleDeleteRequest(teamId: number) {
  const team = teamsStore.teamById(teamId)
  if (team) pendingDeleteTeam.value = team
}
</script>

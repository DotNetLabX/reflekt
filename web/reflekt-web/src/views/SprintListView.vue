<template>
  <div class="flex flex-col items-center p-8">
    <SprintList
      :team-id="teamId"
      :team-name="teamName"
      :sprints="sprintStore.sprints"
      :loading="sprintStore.sprintsLoading"
      :error="sprintStore.sprintsError"
      @select="(id) => router.push({ name: 'sprint', params: { teamId, sprintId: id } })"
      @retry="sprintStore.fetchTeamSprints(teamId)"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useTeamsStore } from '@/stores/teams'
import { useSprintStore } from '@/stores/sprint'
import SprintList from '@/components/sprints/SprintList.vue'

const router = useRouter()
const route = useRoute()
const teamsStore = useTeamsStore()
const sprintStore = useSprintStore()

const teamId = computed(() => Number(route.params.teamId))
const teamName = computed(() => teamsStore.teamById(teamId.value)?.name ?? '')

watch(
  teamId,
  (id) => {
    if (id) sprintStore.fetchTeamSprints(id)
  },
  { immediate: true },
)
</script>

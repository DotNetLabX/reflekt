import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { isHttpError } from '@/api/client'
import { teamsApi } from '@/api/teams'
import type { TeamSummary } from '@/types/team'

export const useTeamsStore = defineStore('teams', () => {
  const teams = ref<TeamSummary[]>([])
  const loaded = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const hasTeams = computed(() => teams.value.length > 0)
  const isAtLimit = computed(() => teams.value.length >= 10)

  function teamById(id: number): TeamSummary | null {
    return teams.value.find((t) => t.id === id) ?? null
  }

  let teamsRequest = 0

  async function fetchMyTeams() {
    const request = ++teamsRequest
    teams.value = []
    error.value = null
    loading.value = true
    try {
      const result = await teamsApi.list()
      if (request === teamsRequest) teams.value = result
    } catch (e) {
      if (request === teamsRequest) {
        error.value = isHttpError(e) ? 'Failed to load teams' : "Can't reach the server"
      }
    } finally {
      if (request === teamsRequest) {
        loading.value = false
        loaded.value = true
      }
    }
  }

  async function createTeam(name: string): Promise<TeamSummary> {
    const team = await teamsApi.create(name)
    teams.value.push(team)
    return team
  }

  async function deleteTeam(teamId: number, confirmationName: string): Promise<void> {
    await teamsApi.remove(teamId, confirmationName)
    teams.value = teams.value.filter((t) => t.id !== teamId)
  }

  return {
    teams,
    loaded,
    loading,
    error,
    hasTeams,
    isAtLimit,
    teamById,
    fetchMyTeams,
    createTeam,
    deleteTeam,
  }
})

import { defineStore } from 'pinia'
import { ref } from 'vue'
import { isHttpError } from '@/api/client'
import { policyApi } from '@/api/policy'

export const usePolicyStore = defineStore('policy', () => {
  const matrix = ref<Record<string, string[]>>({})
  const loading = ref(false)
  const error = ref<string | null>(null)

  let matrixRequest = 0

  async function load() {
    const request = ++matrixRequest
    matrix.value = {}
    error.value = null
    loading.value = true
    try {
      const entries = await policyApi.phaseActions()
      const next: Record<string, string[]> = {}
      for (const entry of entries) {
        next[entry.phase] = entry.actions
      }
      if (request === matrixRequest) matrix.value = next
    } catch (e) {
      if (request === matrixRequest) {
        error.value = isHttpError(e) ? 'Failed to load the retro rules' : "Can't reach the server"
      }
    } finally {
      if (request === matrixRequest) loading.value = false
    }
  }

  function isAllowed(phase: string, action: string): boolean {
    return matrix.value[phase]?.includes(action) ?? false
  }

  return { matrix, loading, error, load, isAllowed }
})

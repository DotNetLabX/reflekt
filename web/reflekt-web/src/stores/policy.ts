import { defineStore } from 'pinia'

interface PhaseActionsEntry {
  phase: string
  actions: string[]
}

let matrixRequest = 0

export const usePolicyStore = defineStore('policy', {
  state: () => ({
    matrix: {} as Record<string, string[]>,
    loading: false,
    error: null as string | null,
  }),
  actions: {
    async fetch() {
      const request = ++matrixRequest
      this.matrix = {}
      this.error = null
      this.loading = true
      try {
        const res = await fetch('/api/phase-actions')
        if (!res.ok) {
          if (request === matrixRequest) this.error = 'Failed to load the retro rules'
          return
        }
        const entries = (await res.json()) as PhaseActionsEntry[]
        const matrix: Record<string, string[]> = {}
        for (const entry of entries) {
          matrix[entry.phase] = entry.actions
        }
        if (request === matrixRequest) this.matrix = matrix
      } catch {
        if (request === matrixRequest) this.error = "Can't reach the server"
      } finally {
        if (request === matrixRequest) this.loading = false
      }
    },
  },
  getters: {
    isAllowed: (state) => (phase: string, action: string) =>
      state.matrix[phase]?.includes(action) ?? false,
  },
})

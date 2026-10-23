import { defineStore } from 'pinia'
import { ref } from 'vue'
import { describeApiError, isHttpError } from '@/api/client'
import { reportsApi } from '@/api/reports'
import type { ReportResponse } from '@/types/report'

export const useReportStore = defineStore('report', () => {
  const report = ref<ReportResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  let reportRequest = 0

  async function loadReport(fetchReportResponse: () => Promise<ReportResponse>): Promise<void> {
    report.value = null
    error.value = null
    loading.value = true
    const request = ++reportRequest
    try {
      const result = await fetchReportResponse()
      if (request === reportRequest) report.value = result
    } catch (e) {
      if (request === reportRequest) {
        error.value = isHttpError(e)
          ? describeApiError(e, {
              statusMessages: {
                404: 'Sprint not found',
                401: 'Not authorized',
                403: 'Not authorized',
              },
              fallback: 'Failed to load report',
            })
          : "Can't reach the server"
      }
    } finally {
      if (request === reportRequest) loading.value = false
    }
  }

  function fetchReport(teamId: number, sprintId: number): Promise<void> {
    return loadReport(() => reportsApi.forTeam(teamId, sprintId))
  }

  function fetchParticipantReport(sprintId: number): Promise<void> {
    return loadReport(() => reportsApi.forParticipant(sprintId))
  }

  return { report, loading, error, fetchReport, fetchParticipantReport }
})

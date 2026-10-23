import { useReportStore } from '@/stores/report'
import { reportMarkdown } from '@/format/reportMarkdown'

export function useCopyReport() {
  const reportStore = useReportStore()

  async function copyReport(): Promise<boolean> {
    if (!reportStore.report) return false
    await navigator.clipboard.writeText(reportMarkdown(reportStore.report))
    return true
  }

  return { copyReport }
}

import { invoke, isHubConnected } from '@/api/hub'
import { useToast } from './useToast'
import type { TimerMethod } from '@/types/timer'

export function useTimerActions() {
  const { addToast } = useToast()

  // Resolves false only when there is no connection, so the caller keeps its menu open.
  async function run(failure: string, method: string, ...args: unknown[]): Promise<boolean> {
    if (!isHubConnected()) {
      addToast('Not connected to server.', 'error')
      return false
    }
    try {
      await invoke(method, ...args)
    } catch (e: unknown) {
      addToast(e instanceof Error ? e.message : failure, 'error')
    }
    return true
  }

  function runTimerAction(method: TimerMethod, sprintId: number): Promise<boolean> {
    return run('Timer action failed.', method, sprintId)
  }

  function addTime(sprintId: number, minutes: number): Promise<boolean> {
    return run('Failed to add time.', 'AddTime', sprintId, minutes)
  }

  return { runTimerAction, addTime }
}

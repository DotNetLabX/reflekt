import * as signalR from '@microsoft/signalr'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'

let connection: signalR.HubConnection | null = null

export function useSprintHub() {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()

  function build(sprintId: number) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/sprint', {
        accessTokenFactory: () => guestStore.token ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('ParticipantJoined', (payload: { id: number; displayName: string }) => {
      const already = participantStore.participants.some((p) => p.id === payload.id)
      if (!already) {
        participantStore.participants.push({
          id: payload.id,
          sprintId,
          displayName: payload.displayName,
          joinedAt: new Date().toISOString(),
        })
      }
    })

    connection.on('ParticipantLeft', (_payload: { participantId: number }) => {
      // No UI indicator in v1 — participant stays in list
    })

    connection.on('PhaseChanged', (phase: string) => {
      console.log('Phase changed:', phase)
    })
  }

  async function connect(sprintId: number) {
    if (connection) await disconnect()
    build(sprintId)
    await connection!.start()
    await connection!.invoke('JoinSprint', sprintId)
  }

  async function disconnect() {
    if (connection) {
      await connection.stop()
      connection = null
    }
  }

  return { connect, disconnect }
}

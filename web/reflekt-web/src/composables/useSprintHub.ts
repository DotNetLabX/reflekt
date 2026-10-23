import * as signalR from '@microsoft/signalr'
import { setHubConnectionSource } from '@/api/hub'
import { useGuestStore } from '@/stores/guest'
import { useParticipantStore } from '@/stores/participant'
import { useCardStore } from '@/stores/card'
import { useVoteStore } from '@/stores/vote'
import { useSprintStore } from '@/stores/sprint'
import { useCardGroupStore } from '@/stores/cardGroup'
import { useReadinessStore } from '@/stores/readiness'
import { useTimerStore } from '@/stores/timer'
import { usePreviousActionsStore } from '@/stores/previousActions'
import type { Card, EvidenceLink } from '@/types/card'
import type { SprintPhase } from '@/types/domain'
import type {
  ParticipantName,
  ReadinessChangedPayload,
  ReadinessSnapshot,
} from '@/types/participant'
import type { ReviewStatusChangedPayload } from '@/types/report'
import type { ColumnConfigUpdatedPayload } from '@/types/sprint'
import type { TimerPayload } from '@/types/timer'
import type { VoteCountChangedPayload } from '@/types/vote'

let connection: signalR.HubConnection | null = null

export function getHubConnection(): signalR.HubConnection | null {
  return connection
}

setHubConnectionSource(getHubConnection)

export function useSprintHub() {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const cardGroupStore = useCardGroupStore()
  const readinessStore = useReadinessStore()
  const timerStore = useTimerStore()
  const previousActionsStore = usePreviousActionsStore()

  function build(sprintId: number): signalR.HubConnection {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/sprint', {
        accessTokenFactory: () => guestStore.token ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('ParticipantJoined', (payload: ParticipantName) =>
      participantStore.applyParticipantJoined(sprintId, payload),
    )

    connection.on('ParticipantLeft', () => {})

    connection.on('ParticipantKicked', (payload: { participantId: number }) =>
      participantStore.applyParticipantKicked(payload.participantId),
    )

    connection.on('PhaseChanged', (phase: SprintPhase) =>
      sprintStore.applyPhaseChanged(sprintId, phase),
    )

    connection.on('CardCreated', (card: Card) => cardStore.applyCardCreated(card))

    connection.on('CardUpdated', (card: Card) => cardStore.applyCardUpdated(card))

    connection.on('CardDeleted', (payload: { id: number }) =>
      cardStore.applyCardDeleted(payload.id),
    )

    connection.on('EvidenceLinkFetched', (link: EvidenceLink) =>
      cardStore.applyEvidenceLinkFetched(link),
    )

    connection.on('VoteCast', (payload: VoteCountChangedPayload) =>
      voteStore.setVoteCount(payload.groupId, payload.newCount),
    )

    connection.on('VoteRetracted', (payload: VoteCountChangedPayload) =>
      voteStore.setVoteCount(payload.groupId, payload.newCount),
    )

    connection.on('GroupCreated', () => cardGroupStore.fetchGroups(sprintId, { background: true }))

    connection.on('CardsGrouped', () => cardGroupStore.fetchGroups(sprintId, { background: true }))

    connection.on('CardUngrouped', () => cardGroupStore.fetchGroups(sprintId, { background: true }))

    connection.on('GroupDissolved', () =>
      cardGroupStore.fetchGroups(sprintId, { background: true }),
    )

    connection.on('GroupTitleChanged', (payload: { groupId: number; title: string | null }) =>
      cardGroupStore.applyTitleChanged(payload.groupId, payload.title),
    )

    connection.on('GroupFocused', (payload: { groupId: number; previousGroupId: number | null }) =>
      cardGroupStore.applyGroupFocused(payload.groupId),
    )

    connection.on('GroupFocusCleared', () => cardGroupStore.applyFocusCleared())

    connection.on(
      'GroupDiscussedChanged',
      (payload: { groupId: number; isDiscussed: boolean; discussedAt: string | null }) =>
        cardGroupStore.applyDiscussedChanged(payload.groupId, payload.isDiscussed),
    )

    connection.on('ColumnConfigUpdated', (payload: ColumnConfigUpdatedPayload) =>
      sprintStore.applyColumnConfig(payload),
    )

    connection.on('SprintClosed', () => sprintStore.applySprintClosed())

    connection.on('ReadinessChanged', (payload: ReadinessChangedPayload) =>
      readinessStore.applyChange(payload),
    )

    connection.on('ReadinessReset', () => readinessStore.reset())

    connection.on('ReadinessSnapshot', (payload: ReadinessSnapshot) =>
      readinessStore.setFromSnapshot(payload),
    )

    connection.on('TimerUpdated', (payload: TimerPayload) => timerStore.applyTimer(payload))

    connection.on('TimerSnapshot', (payload: TimerPayload) => timerStore.applyTimer(payload))

    connection.on('ReviewStatusChanged', (payload: ReviewStatusChangedPayload) =>
      previousActionsStore.applyReviewStatusChanged(payload),
    )

    return connection
  }

  async function connect(sprintId: number) {
    if (connection) await disconnect()
    const built = build(sprintId)
    await built.start()
    await built.invoke('JoinSprint', sprintId)
  }

  async function disconnect() {
    if (connection) {
      await connection.stop()
      connection = null
    }
  }

  return { connect, disconnect }
}

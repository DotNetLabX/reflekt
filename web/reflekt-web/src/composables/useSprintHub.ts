import * as signalR from '@microsoft/signalr'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore, type Card, type EvidenceLink } from '../stores/card'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import { useCardGroupStore } from '../stores/cardGroup'

let connection: signalR.HubConnection | null = null

export function useSprintHub(onKicked?: () => void) {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const cardGroupStore = useCardGroupStore()

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

    connection.on('ParticipantLeft', (_payload: { participantId: number }) => {})

    connection.on('ParticipantKicked', (payload: { participantId: number }) => {
      participantStore.participants = participantStore.participants.filter(
        (p) => p.id !== payload.participantId,
      )
      if (onKicked && participantStore.currentParticipant?.id === payload.participantId) {
        onKicked()
      }
    })

    connection.on('PhaseChanged', (phase: string) => {
      const wasWriting = sprintStore.currentSummary?.currentPhase === 'Writing'
      if (sprintStore.currentSummary) {
        sprintStore.currentSummary.currentPhase = phase
      }
      if (wasWriting && phase === 'Grouping') {
        cardStore.fetchCards(sprintId)
        cardGroupStore.fetchGroups(sprintId)
      } else if (phase !== 'Writing') {
        cardStore.fetchCards(sprintId)
        cardGroupStore.fetchGroups(sprintId)
      }
    })

    connection.on('CardCreated', (card: Card) => {
      if (!cardStore.cards.some((c) => c.id === card.id)) {
        cardStore.cards.unshift(card)
      }
    })

    connection.on('CardUpdated', (card: Card) => {
      const idx = cardStore.cards.findIndex((c) => c.id === card.id)
      if (idx !== -1) cardStore.cards[idx] = card
    })

    connection.on('CardDeleted', (payload: { id: number }) => {
      cardStore.cards = cardStore.cards.filter((c) => c.id !== payload.id)
    })

    connection.on('EvidenceLinkFetched', (link: EvidenceLink) => {
      const card = cardStore.cards.find((c) => c.id === link.cardId)
      if (card) {
        const idx = card.evidenceLinks.findIndex((e) => e.id === link.id)
        if (idx !== -1) card.evidenceLinks[idx] = link
        else card.evidenceLinks.push(link)
      }
    })

    connection.on('VoteCast', (payload: { groupId: number; newCount: number }) => {
      voteStore.setVoteCount(payload.groupId, payload.newCount)
    })

    connection.on('VoteRetracted', (payload: { groupId: number; newCount: number }) => {
      voteStore.setVoteCount(payload.groupId, payload.newCount)
    })

    connection.on('GroupCreated', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('CardsGrouped', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('CardUngrouped', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('GroupDissolved', () => {
      cardGroupStore.fetchGroups(sprintId)
    })

    connection.on('GroupTitleChanged', (payload: { groupId: number; title: string | null }) => {
      const group = cardGroupStore.groups.find((g) => g.id === payload.groupId)
      if (group) group.title = payload.title
    })

    connection.on('GroupFocused', (payload: { groupId: number; previousGroupId: number | null }) => {
      cardGroupStore.groups.forEach((g) => (g.isFocused = g.id === payload.groupId))
    })

    connection.on('GroupFocusCleared', (_payload: { previousGroupId: number | null }) => {
      cardGroupStore.groups.forEach((g) => (g.isFocused = false))
    })

    connection.on(
      'GroupDiscussedChanged',
      (payload: { groupId: number; isDiscussed: boolean; discussedAt: string | null }) => {
        const group = cardGroupStore.groups.find((g) => g.id === payload.groupId)
        if (group) group.isDiscussed = payload.isDiscussed
      },
    )
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

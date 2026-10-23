import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { cardsApi } from '@/api/cards'
import { isHttpError } from '@/api/client'
import { useParticipantStore } from './participant'
import { createPendingValues } from './pendingValues'
import type { Card, CreateCardPayload, EditCardPayload, EvidenceLink } from '@/types/card'

type CardContent = Pick<Card, 'text' | 'assigneeName' | 'updatedAt'>

function contentOf(card: Card): CardContent {
  return { text: card.text, assigneeName: card.assigneeName, updatedAt: card.updatedAt }
}

export const useCardStore = defineStore('card', () => {
  const participantStore = useParticipantStore()

  const cards = ref<Card[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  const column1Cards = computed(() => cards.value.filter((c) => c.columnType === 'Column1'))
  const column2Cards = computed(() => cards.value.filter((c) => c.columnType === 'Column2'))
  const column3Cards = computed(() => cards.value.filter((c) => c.columnType === 'Column3'))
  const actionItemCards = computed(() => cards.value.filter((c) => c.columnType === 'ActionItems'))

  let cardsRequest = 0
  let lastTempId = 0
  const pendingCreates = new Set<number>()
  const pendingDeletes = new Map<number, { card: Card; index: number }>()
  const pendingDetaches = new Map<number, { cardId: number; link: EvidenceLink; index: number }>()
  const pendingEdits = createPendingValues<number, CardContent>((cardId, content) => {
    const index = cards.value.findIndex((c) => c.id === cardId)
    const card = cards.value[index]
    if (card) cards.value[index] = { ...card, ...content }
    const deleting = pendingDeletes.get(cardId)
    if (deleting) deleting.card = { ...deleting.card, ...content }
  })

  function findCard(cardId: number): Card | undefined {
    return cards.value.find((c) => c.id === cardId)
  }

  function isPendingCard(card: Card): boolean {
    return card.id < 0
  }

  function settleLinkList(cardId: number): void {
    for (const [linkId, detach] of pendingDetaches) {
      if (detach.cardId === cardId) pendingDetaches.delete(linkId)
    }
  }

  // A background refresh keeps the cards on screen and keeps a previous refresh's
  // error until one succeeds.
  async function fetchCards(sprintId: number, { background = false } = {}): Promise<void> {
    const request = ++cardsRequest
    if (!background) {
      cards.value = []
      error.value = null
      loading.value = true
    }
    try {
      const result = await cardsApi.list(sprintId)
      if (request === cardsRequest) {
        cards.value = result
        error.value = null
        pendingCreates.clear()
        pendingDeletes.clear()
        pendingDetaches.clear()
        pendingEdits.settleAll()
      }
    } catch (e) {
      if (request === cardsRequest) {
        error.value = isHttpError(e) ? 'Failed to load cards' : "Can't reach the server"
      }
    } finally {
      if (request === cardsRequest) loading.value = false
    }
  }

  async function createCard(sprintId: number, payload: CreateCardPayload): Promise<Card> {
    const now = new Date().toISOString()
    const tempId = --lastTempId
    cards.value.unshift({
      id: tempId,
      sprintId,
      participantId: participantStore.currentParticipant?.id ?? 0,
      columnType: payload.columnType,
      cardType: payload.cardType,
      text: payload.text,
      assigneeName: payload.assigneeName ?? null,
      groupId: null,
      createdAt: now,
      updatedAt: now,
      voteCount: 0,
      evidenceLinks: [],
    })
    pendingCreates.add(tempId)
    try {
      const created = await cardsApi.create(sprintId, payload)
      replaceTempCard(tempId, created)
      return created
    } catch (e) {
      pendingCreates.delete(tempId)
      cards.value = cards.value.filter((c) => c.id !== tempId)
      throw e
    }
  }

  function replaceTempCard(tempId: number, card: Card): void {
    pendingCreates.delete(tempId)
    const index = cards.value.findIndex((c) => c.id === tempId)
    if (index === -1) return
    if (findCard(card.id)) cards.value.splice(index, 1)
    else cards.value[index] = card
  }

  async function editCard(
    sprintId: number,
    cardId: number,
    payload: EditCardPayload,
  ): Promise<Card> {
    const card = findCard(cardId)
    const change = card
      ? pendingEdits.change(cardId, contentOf(card), {
          text: payload.text,
          assigneeName: payload.assigneeName ?? null,
          updatedAt: new Date().toISOString(),
        })
      : null
    try {
      const updated = await cardsApi.edit(sprintId, cardId, payload)
      if (change) pendingEdits.confirm(cardId, change, contentOf(updated))
      return updated
    } catch (e) {
      if (change) pendingEdits.undo(cardId, change)
      throw e
    }
  }

  async function deleteCard(sprintId: number, cardId: number): Promise<void> {
    const index = cards.value.findIndex((c) => c.id === cardId)
    const card = cards.value[index]
    if (card) {
      pendingDeletes.set(cardId, { card, index })
      cards.value.splice(index, 1)
    }
    try {
      await cardsApi.remove(sprintId, cardId)
      pendingDeletes.delete(cardId)
      pendingEdits.settle(cardId)
      cards.value = cards.value.filter((c) => c.id !== cardId)
    } catch (e) {
      const pending = pendingDeletes.get(cardId)
      pendingDeletes.delete(cardId)
      if (pending && !findCard(cardId)) {
        cards.value.splice(Math.min(pending.index, cards.value.length), 0, pending.card)
      }
      throw e
    }
  }

  async function attachEvidenceLink(sprintId: number, cardId: number, url: string): Promise<void> {
    const card = findCard(cardId)
    if (!card) return
    const tempId = --lastTempId
    card.evidenceLinks.push({
      id: tempId,
      cardId,
      url,
      source: 'Generic',
      title: null,
      description: null,
      iconUrl: null,
      imageUrl: null,
      externalKey: null,
      fetchedAt: null,
      fetchStatus: 'Pending',
    })
    try {
      const link = await cardsApi.attachEvidenceLink(sprintId, cardId, url)
      const current = findCard(cardId)
      if (!current) return
      const index = current.evidenceLinks.findIndex((l) => l.id === tempId)
      if (index === -1) return
      if (current.evidenceLinks.some((l) => l.id === link.id))
        current.evidenceLinks.splice(index, 1)
      else current.evidenceLinks[index] = link
    } catch (e) {
      const current = findCard(cardId)
      if (current) current.evidenceLinks = current.evidenceLinks.filter((l) => l.id !== tempId)
      throw e
    }
  }

  async function detachEvidenceLink(
    sprintId: number,
    cardId: number,
    evidenceLinkId: number,
  ): Promise<void> {
    const card = findCard(cardId)
    if (!card) return
    const index = card.evidenceLinks.findIndex((l) => l.id === evidenceLinkId)
    const link = card.evidenceLinks[index]
    if (link) {
      pendingDetaches.set(evidenceLinkId, { cardId, link, index })
      card.evidenceLinks.splice(index, 1)
    }
    try {
      await cardsApi.detachEvidenceLink(sprintId, cardId, evidenceLinkId)
      pendingDetaches.delete(evidenceLinkId)
    } catch (e) {
      const pending = pendingDetaches.get(evidenceLinkId)
      pendingDetaches.delete(evidenceLinkId)
      const current = findCard(cardId)
      if (pending && current && !current.evidenceLinks.some((l) => l.id === evidenceLinkId)) {
        current.evidenceLinks.splice(
          Math.min(pending.index, current.evidenceLinks.length),
          0,
          pending.link,
        )
      }
      throw e
    }
  }

  function applyCardCreated(card: Card): void {
    const index = cards.value.findIndex((c) => c.id === card.id)
    if (index !== -1) {
      pendingEdits.settle(card.id)
      cards.value[index] = card
      return
    }
    const tempIndex = cards.value.findIndex(
      (c) =>
        pendingCreates.has(c.id) &&
        c.participantId === card.participantId &&
        c.columnType === card.columnType &&
        c.cardType === card.cardType &&
        c.text === card.text,
    )
    const temp = cards.value[tempIndex]
    if (temp) {
      pendingCreates.delete(temp.id)
      cards.value[tempIndex] = card
    } else {
      cards.value.unshift(card)
    }
  }

  function applyCardUpdated(card: Card): void {
    pendingEdits.settle(card.id)
    settleLinkList(card.id)
    const index = cards.value.findIndex((c) => c.id === card.id)
    if (index !== -1) cards.value[index] = card
    const deleting = pendingDeletes.get(card.id)
    if (deleting) deleting.card = card
  }

  function applyCardDeleted(cardId: number): void {
    pendingDeletes.delete(cardId)
    pendingEdits.settle(cardId)
    settleLinkList(cardId)
    cards.value = cards.value.filter((c) => c.id !== cardId)
  }

  function applyEvidenceLinkFetched(link: EvidenceLink): void {
    pendingDetaches.delete(link.id)
    const card = findCard(link.cardId) ?? pendingDeletes.get(link.cardId)?.card
    if (!card) return
    const index = card.evidenceLinks.findIndex((l) => l.id === link.id)
    if (index !== -1) card.evidenceLinks[index] = link
    else card.evidenceLinks.push(link)
  }

  return {
    cards,
    loading,
    error,
    column1Cards,
    column2Cards,
    column3Cards,
    actionItemCards,
    isPendingCard,
    fetchCards,
    createCard,
    editCard,
    deleteCard,
    attachEvidenceLink,
    detachEvidenceLink,
    applyCardCreated,
    applyCardUpdated,
    applyCardDeleted,
    applyEvidenceLinkFetched,
  }
})

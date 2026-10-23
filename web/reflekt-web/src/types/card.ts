import type { CardType, ColumnType, FetchStatus } from './domain'

export interface EvidenceLink {
  id: number
  cardId: number
  url: string
  source: string
  title: string | null
  description: string | null
  iconUrl: string | null
  imageUrl: string | null
  externalKey: string | null
  fetchedAt: string | null
  fetchStatus: FetchStatus
}

export interface Card {
  id: number
  sprintId: number
  participantId: number
  columnType: ColumnType
  cardType: CardType
  text: string
  assigneeName: string | null
  groupId: number | null
  createdAt: string
  updatedAt: string
  voteCount: number
  evidenceLinks: EvidenceLink[]
}

export interface CreateCardPayload {
  text: string
  columnType: ColumnType
  cardType: CardType
  assigneeName?: string | null
}

export interface EditCardPayload {
  text: string
  assigneeName?: string | null
}

export interface CardGroupCard {
  id: number
  text: string
  createdAt: string
  evidenceLinks: Pick<EvidenceLink, 'id' | 'url' | 'source' | 'title'>[]
}

export interface CardGroup {
  id: number
  sprintId: number
  columnType: ColumnType
  title: string | null
  cards: CardGroupCard[]
  voteCount: number
  isDiscussed: boolean
  isFocused: boolean
}

export interface DragPayload {
  type: 'group' | 'card'
  groupId: number
  columnType: ColumnType
  cardIds: number[]
  cardId?: number
  sourceGroupId?: number
}

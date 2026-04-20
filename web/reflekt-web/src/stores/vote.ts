import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface VoteDistribution {
  cardId: number
  count: number
}

export const useVoteStore = defineStore('vote', {
  state: () => ({
    myVotes: [] as VoteDistribution[],
    voteCounts: new Map<number, number>(),
    votesPerParticipant: 3,
  }),
  getters: {
    remainingBudget: (state): number => {
      const used = state.myVotes.reduce((sum, v) => sum + v.count, 0)
      return state.votesPerParticipant - used
    },
    myVotesOnCard:
      (state) =>
      (cardId: number): number =>
        state.myVotes.find((v) => v.cardId === cardId)?.count ?? 0,
    voteCountForCard:
      (state) =>
      (cardId: number): number =>
        state.voteCounts.get(cardId) ?? 0,
  },
  actions: {
    async fetchMyVotes(sprintId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return
      const res = await fetch(`/api/sprints/${sprintId}/votes/mine`, {
        headers: { 'X-Guest-Token': guestStore.token },
      })
      if (!res.ok) return
      this.myVotes = (await res.json()) as VoteDistribution[]
    },

    async castVote(sprintId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return

      const prev = this.myVotes.find((v) => v.cardId === cardId)
      const prevCount = prev?.count ?? 0
      const prevTotal = this.voteCounts.get(cardId) ?? 0

      // Optimistic update
      if (prev) {
        prev.count++
      } else {
        this.myVotes.push({ cardId, count: 1 })
      }
      this.voteCounts.set(cardId, prevTotal + 1)

      try {
        const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/votes`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Guest-Token': guestStore.token },
          body: JSON.stringify({}),
        })
        if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
        const data = await res.json()
        this.voteCounts.set(cardId, data.voteCount)
        const entry = this.myVotes.find((v) => v.cardId === cardId)
        if (entry) entry.count = data.myVoteCount
      } catch (err) {
        // Revert optimistic update
        const entry = this.myVotes.find((v) => v.cardId === cardId)
        if (entry) {
          entry.count = prevCount
          if (entry.count === 0) this.myVotes = this.myVotes.filter((v) => v.cardId !== cardId)
        }
        this.voteCounts.set(cardId, prevTotal)
        throw err
      }
    },

    async retractVote(sprintId: number, cardId: number): Promise<void> {
      const guestStore = useGuestStore()
      if (!guestStore.token) return

      const prev = this.myVotes.find((v) => v.cardId === cardId)
      const prevCount = prev?.count ?? 0
      const prevTotal = this.voteCounts.get(cardId) ?? 0

      // Optimistic update
      if (prev && prev.count > 1) {
        prev.count--
      } else {
        this.myVotes = this.myVotes.filter((v) => v.cardId !== cardId)
      }
      this.voteCounts.set(cardId, Math.max(0, prevTotal - 1))

      try {
        const res = await fetch(`/api/sprints/${sprintId}/cards/${cardId}/votes`, {
          method: 'DELETE',
          headers: { 'X-Guest-Token': guestStore.token },
        })
        if (!res.ok) throw { status: res.status, body: await res.json().catch(() => ({})) }
        const data = await res.json()
        this.voteCounts.set(cardId, data.voteCount)
        const entry = this.myVotes.find((v) => v.cardId === cardId)
        if (entry) entry.count = data.myVoteCount
        else if (data.myVoteCount > 0) this.myVotes.push({ cardId, count: data.myVoteCount })
      } catch (err) {
        // Revert optimistic update
        const entry = this.myVotes.find((v) => v.cardId === cardId)
        if (entry) {
          entry.count = prevCount
        } else if (prevCount > 0) {
          this.myVotes.push({ cardId, count: prevCount })
        }
        this.voteCounts.set(cardId, prevTotal)
        throw err
      }
    },

    setVoteCount(cardId: number, count: number): void {
      this.voteCounts.set(cardId, count)
    },

    initVoteCounts(counts: { cardId: number; voteCount: number }[]): void {
      for (const { cardId, voteCount } of counts) {
        this.voteCounts.set(cardId, voteCount)
      }
    },
  },
})

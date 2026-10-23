import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { isHttpError } from '@/api/client'
import { votesApi } from '@/api/votes'
import { useGuestStore } from './guest'
import type { VoteDistribution, VoteResponse } from '@/types/vote'

export const useVoteStore = defineStore('vote', () => {
  const guestStore = useGuestStore()

  const myVotes = ref<VoteDistribution[]>([])
  const voteCounts = ref(new Map<number, number>())
  const votesPerParticipant = ref(3)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const remainingBudget = computed(() => {
    const used = myVotes.value.reduce((sum, v) => sum + v.count, 0)
    return votesPerParticipant.value - used
  })

  function myVotesOnGroup(groupId: number): number {
    return myVotes.value.find((v) => v.groupId === groupId)?.count ?? 0
  }

  function voteCountForGroup(groupId: number): number {
    return voteCounts.value.get(groupId) ?? 0
  }

  // One optimistic vote: the amounts it added to the group's total and to the user's own
  // count. A settled amount was replaced by the server's value and is never taken back.
  interface VoteChange {
    total: number
    own: number
    totalSettled: boolean
    ownSettled: boolean
  }
  const pendingVotes = new Map<number, VoteChange[]>()

  function setMyVotesOnGroup(groupId: number, count: number): void {
    const entry = myVotes.value.find((v) => v.groupId === groupId)
    if (count <= 0) myVotes.value = myVotes.value.filter((v) => v.groupId !== groupId)
    else if (entry) entry.count = count
    else myVotes.value.push({ groupId, count })
  }

  function addVote(groupId: number, step: 1 | -1): VoteChange {
    const total = voteCountForGroup(groupId)
    const own = myVotesOnGroup(groupId)
    const nextTotal = Math.max(0, total + step)
    const nextOwn = Math.max(0, own + step)
    voteCounts.value.set(groupId, nextTotal)
    setMyVotesOnGroup(groupId, nextOwn)
    const change = {
      total: nextTotal - total,
      own: nextOwn - own,
      totalSettled: false,
      ownSettled: false,
    }
    const list = pendingVotes.get(groupId)
    if (list) list.push(change)
    else pendingVotes.set(groupId, [change])
    return change
  }

  // The response settles this change and every older one on the group; a newer change still
  // in flight stays on screen on top of the server's values. A change a newer response already
  // settled is gone from the list, and its own response is older than what is on screen. A
  // settled total was replaced by a VoteCast/VoteRetracted broadcast; a settled own count was
  // replaced by a fetchMyVotes snapshot, possibly of another sprint.
  function confirmVote(groupId: number, change: VoteChange, response: VoteResponse): void {
    const list = pendingVotes.get(groupId)
    const index = list?.indexOf(change) ?? -1
    if (!list || index === -1) return
    list.splice(0, index + 1)
    if (list.length === 0) pendingVotes.delete(groupId)
    if (!change.totalSettled) {
      const newerTotal = list.reduce((sum, c) => sum + (c.totalSettled ? 0 : c.total), 0)
      voteCounts.value.set(groupId, Math.max(0, response.voteCount + newerTotal))
    }
    if (!change.ownSettled) {
      const newerOwn = list.reduce((sum, c) => sum + (c.ownSettled ? 0 : c.own), 0)
      setMyVotesOnGroup(groupId, response.myVoteCount + newerOwn)
    }
  }

  function undoVote(groupId: number, change: VoteChange): void {
    const list = pendingVotes.get(groupId)
    const index = list?.indexOf(change) ?? -1
    if (!list || index === -1) return
    list.splice(index, 1)
    if (list.length === 0) pendingVotes.delete(groupId)
    if (!change.totalSettled) {
      voteCounts.value.set(groupId, Math.max(0, voteCountForGroup(groupId) - change.total))
    }
    if (!change.ownSettled) setMyVotesOnGroup(groupId, myVotesOnGroup(groupId) - change.own)
  }

  function settleTotals(groupId: number): void {
    pendingVotes.get(groupId)?.forEach((c) => (c.totalSettled = true))
  }

  let myVotesRequest = 0

  async function fetchMyVotes(sprintId: number): Promise<void> {
    if (!guestStore.token) return
    const request = ++myVotesRequest
    myVotes.value = []
    error.value = null
    loading.value = true
    try {
      const result = await votesApi.mine(sprintId)
      if (request === myVotesRequest) {
        myVotes.value = result
        pendingVotes.forEach((list) => list.forEach((c) => (c.ownSettled = true)))
      }
    } catch (e) {
      if (request === myVotesRequest) {
        error.value = isHttpError(e) ? 'Failed to load your votes' : "Can't reach the server"
      }
    } finally {
      if (request === myVotesRequest) loading.value = false
    }
  }

  async function castVote(sprintId: number, groupId: number): Promise<void> {
    if (!guestStore.token) return
    const change = addVote(groupId, 1)
    try {
      confirmVote(groupId, change, await votesApi.cast(sprintId, groupId))
    } catch (err) {
      undoVote(groupId, change)
      throw err
    }
  }

  async function retractVote(sprintId: number, groupId: number): Promise<void> {
    if (!guestStore.token) return
    const change = addVote(groupId, -1)
    try {
      confirmVote(groupId, change, await votesApi.retract(sprintId, groupId))
    } catch (err) {
      undoVote(groupId, change)
      throw err
    }
  }

  function setVotesPerParticipant(count: number): void {
    votesPerParticipant.value = count
  }

  function setVoteCount(groupId: number, count: number): void {
    settleTotals(groupId)
    voteCounts.value.set(groupId, count)
  }

  function initVoteCounts(counts: { groupId: number; voteCount: number }[]): void {
    for (const { groupId, voteCount } of counts) {
      settleTotals(groupId)
      voteCounts.value.set(groupId, voteCount)
    }
  }

  return {
    myVotes,
    voteCounts,
    votesPerParticipant,
    loading,
    error,
    remainingBudget,
    myVotesOnGroup,
    voteCountForGroup,
    fetchMyVotes,
    castVote,
    retractVote,
    setVotesPerParticipant,
    setVoteCount,
    initVoteCounts,
  }
})

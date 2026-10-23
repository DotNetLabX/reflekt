import { defineStore } from 'pinia'
import { ref } from 'vue'
import { isHttpError } from '@/api/client'
import { participantsApi } from '@/api/participants'
import type { JoinSprintResponse, Participant, ParticipantName } from '@/types/participant'

export const useParticipantStore = defineStore('participant', () => {
  const currentParticipant = ref<Participant | null>(null)
  const participants = ref<Participant[]>([])
  const meLoading = ref(false)
  const meError = ref<string | null>(null)
  const meStatus = ref<number | null>(null)
  const participantsLoading = ref(false)
  const participantsError = ref<string | null>(null)
  const kicked = ref(false)

  let meRequest = 0
  let participantsRequest = 0
  // Shared by every action that sets currentParticipant: a response that lands after a
  // later identity action started, or after the room abandoned its pending ones, is dropped.
  let identityRequest = 0

  async function join(sprintId: number, displayName: string): Promise<JoinSprintResponse> {
    const identity = ++identityRequest
    kicked.value = false
    const data = await participantsApi.join(sprintId, displayName)
    if (identity === identityRequest) currentParticipant.value = data
    return data
  }

  async function joinWithGoogle(sprintId: number): Promise<JoinSprintResponse> {
    const identity = ++identityRequest
    kicked.value = false
    const data = await participantsApi.joinWithGoogle(sprintId)
    if (identity === identityRequest) currentParticipant.value = data
    return data
  }

  function abandonPendingIdentity(): void {
    identityRequest++
  }

  async function fetchMe(sprintId: number): Promise<void> {
    const request = ++meRequest
    const identity = ++identityRequest
    currentParticipant.value = null
    kicked.value = false
    meError.value = null
    meStatus.value = null
    meLoading.value = true
    try {
      const participant = await participantsApi.me(sprintId)
      if (identity === identityRequest) currentParticipant.value = participant
    } catch (e) {
      if (request === meRequest) {
        if (isHttpError(e)) {
          meError.value = 'Failed to load your place in this retro'
          meStatus.value = e.status
        } else {
          meError.value = "Can't reach the server"
        }
      }
    } finally {
      if (request === meRequest) meLoading.value = false
    }
  }

  async function fetchParticipants(sprintId: number): Promise<void> {
    const request = ++participantsRequest
    participants.value = []
    participantsError.value = null
    participantsLoading.value = true
    try {
      const result = await participantsApi.list(sprintId)
      if (request === participantsRequest) participants.value = result
    } catch (e) {
      if (request === participantsRequest) {
        participantsError.value = isHttpError(e)
          ? 'Failed to load participants'
          : "Can't reach the server"
      }
    } finally {
      if (request === participantsRequest) participantsLoading.value = false
    }
  }

  async function kickParticipant(sprintId: number, participantId: number): Promise<void> {
    await participantsApi.kick(sprintId, participantId)
    participants.value = participants.value.filter((p) => p.id !== participantId)
  }

  function applyParticipantJoined(sprintId: number, joined: ParticipantName): void {
    if (participants.value.some((p) => p.id === joined.id)) return
    participants.value.push({
      id: joined.id,
      sprintId,
      displayName: joined.displayName,
      joinedAt: new Date().toISOString(),
    })
  }

  function applyParticipantKicked(participantId: number): void {
    participants.value = participants.value.filter((p) => p.id !== participantId)
    if (currentParticipant.value?.id === participantId) kicked.value = true
  }

  return {
    currentParticipant,
    participants,
    meLoading,
    meError,
    meStatus,
    participantsLoading,
    participantsError,
    kicked,
    join,
    joinWithGoogle,
    abandonPendingIdentity,
    fetchMe,
    fetchParticipants,
    kickParticipant,
    applyParticipantJoined,
    applyParticipantKicked,
  }
})

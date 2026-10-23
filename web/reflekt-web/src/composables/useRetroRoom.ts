import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useGuestStore } from '../stores/guest'
import { useParticipantStore } from '../stores/participant'
import { useCardStore } from '../stores/card'
import { useAuthStore } from '../stores/auth'
import { getHubConnection, useSprintHub } from './useSprintHub'
import { useVoteStore } from '../stores/vote'
import { useSprintStore } from '../stores/sprint'
import { usePolicyStore } from '../stores/policy'
import { useCardGroupStore } from '../stores/cardGroup'
import { useReportStore } from '../stores/report'
import { useReadinessStore } from '../stores/readiness'
import { usePreviousActionsStore } from '../stores/previousActions'

type EntryStage = 'info' | 'identity' | 'data'
type ConnectOutcome = 'ok' | 'failed' | 'stale'

// Shared by every room: the hub connection is module-wide, so its connect and
// disconnect calls must run one at a time across rooms.
let hubLock: Promise<unknown> = Promise.resolve()

function withHubLock<T>(op: () => Promise<T>): Promise<T> {
  const run = hubLock.then(op)
  hubLock = run.catch(() => undefined)
  return run
}

export function useRetroRoom(sprintId: number) {
  const guestStore = useGuestStore()
  const participantStore = useParticipantStore()
  const cardStore = useCardStore()
  const authStore = useAuthStore()
  const voteStore = useVoteStore()
  const sprintStore = useSprintStore()
  const policyStore = usePolicyStore()
  const cardGroupStore = useCardGroupStore()
  const reportStore = useReportStore()
  const readinessStore = useReadinessStore()

  const previousActionsStore = usePreviousActionsStore()

  const kicked = ref(false)
  const busy = ref(false)
  const errorMessage = ref('')
  const joined = ref(false)
  const loading = ref(true)
  const roomError = ref<string | null>(null)

  let resumeStage: EntryStage = 'info'
  let entryRunning = false
  // Raised by every new entry and by unmount; an entry whose number is no longer
  // current must not touch the shared stores or the guest token, and may stop only the
  // hub connection it built itself.
  let currentEntry = 0
  const superseded = (entry: number) => entry !== currentEntry
  let ownedConnection: ReturnType<typeof getHubConnection> = null
  let connectInFlight = false

  const { connect, disconnect } = useSprintHub(() => {
    kicked.value = true
    guestStore.clearToken(sprintId)
    cancelHub()
  })

  // Call only while holding the hub lock.
  async function stopOwned(): Promise<void> {
    if (ownedConnection && getHubConnection() === ownedConnection) await disconnect()
    ownedConnection = null
  }

  function releaseHub(): Promise<void> {
    return withHubLock(stopOwned)
  }

  // A pending join holds the hub lock and may never be answered, so leaving stops this
  // room's own connection directly instead of queuing behind the lock.
  function cancelHub(): void {
    if (connectInFlight && ownedConnection) ownedConnection.stop().catch(() => undefined)
    releaseHub()
  }

  function connectHub(entry: number): Promise<ConnectOutcome> {
    return withHubLock(async () => {
      if (superseded(entry)) return 'stale'
      if (getHubConnection()) await disconnect()
      if (superseded(entry)) return 'stale'
      const pending = connect(sprintId)
      ownedConnection = getHubConnection()
      connectInFlight = true
      let failed = false
      try {
        await pending
      } catch {
        failed = true
      }
      connectInFlight = false
      if (superseded(entry)) {
        await stopOwned()
        return 'stale'
      }
      return failed ? 'failed' : 'ok'
    })
  }

  const sprintName = computed(() => sprintStore.currentSummary?.name ?? '')
  const currentPhase = computed(() => sprintStore.currentSummary?.currentPhase ?? '')
  const summaryParticipants = computed(() => sprintStore.currentSummary?.participants ?? [])
  const isReportingPhase = computed(() => currentPhase.value === 'Reporting')
  const isClosed = computed(() => sprintStore.currentSummary?.state === 'Closed')
  const teamId = computed(() => sprintStore.currentSummary?.teamId ?? null)
  const isScrumMaster = computed(() => sprintStore.currentSummary?.isScrumMaster ?? false)

  // After entry, cards and groups load only as background refreshes, so their error
  // is a failed refresh of a board that is still on screen.
  const refreshError = computed(() => (joined.value ? (cardStore.error ?? cardGroupStore.error) : null))

  function failStage(stage: EntryStage, message: string) {
    resumeStage = stage
    roomError.value = message
    loading.value = false
  }

  async function loadRoomInfo(entry: number): Promise<boolean> {
    await Promise.all([policyStore.fetch(), sprintStore.fetchSprintSummary(sprintId)])
    if (superseded(entry)) return false
    const error = policyStore.error ?? sprintStore.currentSummaryError
    if (error) {
      failStage('info', error)
      return false
    }
    return true
  }

  async function identify(entry: number): Promise<boolean> {
    if (!authStore.loaded) await authStore.fetchMe()
    if (superseded(entry)) return false
    if (authStore.isSignedIn) {
      try {
        const response = await participantStore.joinWithGoogle(sprintId)
        if (superseded(entry)) return false
        guestStore.saveToken(sprintId, response.guestToken, response.displayName)
        return true
      } catch (err: unknown) {
        if (superseded(entry)) return false
        console.error('[joinWithGoogle] failed:', err)
        const status = (err as { status?: number })?.status
        if (status === 409) errorMessage.value = 'Sprint is not accepting participants.'
        else if (status === 404) errorMessage.value = 'Sprint not found.'
        else errorMessage.value = `Could not join with Google (${status ?? 'network error'}). You can join as a guest instead.`
      }
      loading.value = false
      return false
    }

    guestStore.loadToken(sprintId)
    if (guestStore.token) {
      await participantStore.fetchMe(sprintId)
      if (superseded(entry)) return false
      if (!participantStore.meError) return true
      if (participantStore.meStatus !== 401) {
        failStage('identity', participantStore.meError)
        return false
      }
      guestStore.clearToken(sprintId)
    }
    loading.value = false
    return false
  }

  async function loadRoomData(entry: number): Promise<void> {
    loading.value = true
    await Promise.all([
      participantStore.fetchParticipants(sprintId),
      cardStore.fetchCards(sprintId),
      cardGroupStore.fetchGroups(sprintId),
      sprintStore.fetchSprintSummary(sprintId),
      previousActionsStore.fetchPreviousActions(sprintId),
    ])
    if (superseded(entry)) return
    const error =
      participantStore.participantsError ??
      cardStore.error ??
      cardGroupStore.error ??
      sprintStore.currentSummaryError ??
      previousActionsStore.error
    if (error) {
      failStage('data', error)
      return
    }
    if (sprintStore.currentSummary) voteStore.votesPerParticipant = sprintStore.currentSummary.votesPerParticipant
    await voteStore.fetchMyVotes(sprintId)
    if (superseded(entry)) return
    if (voteStore.error) {
      failStage('data', voteStore.error)
      return
    }
    readinessStore.reset()
    const outcome = await connectHub(entry)
    if (outcome === 'stale') return
    if (outcome === 'failed') {
      failStage('data', "Can't connect to the live board")
      return
    }
    joined.value = true
    loading.value = false
    if (isReportingPhase.value) reportStore.fetchParticipantReport(sprintId)
  }

  async function runEntry(from: EntryStage): Promise<void> {
    const entry = ++currentEntry
    entryRunning = true
    try {
      if (from === 'info' && !(await loadRoomInfo(entry))) return
      if (from !== 'data' && !(await identify(entry))) return
      await loadRoomData(entry)
    } finally {
      if (!superseded(entry)) entryRunning = false
    }
  }

  async function retryEntry(): Promise<void> {
    if (entryRunning) return
    roomError.value = null
    loading.value = true
    await runEntry(resumeStage)
  }

  async function retryRefresh(): Promise<void> {
    await Promise.all([
      cardStore.fetchCards(sprintId, { background: true }),
      cardGroupStore.fetchGroups(sprintId, { background: true }),
    ])
  }

  async function handleJoin(displayName: string): Promise<void> {
    const entry = currentEntry
    busy.value = true
    errorMessage.value = ''
    try {
      const response = await participantStore.join(sprintId, displayName.trim())
      if (superseded(entry)) {
        busy.value = false
        return
      }
      guestStore.saveToken(sprintId, response.guestToken, response.displayName)
    } catch (err: unknown) {
      busy.value = false
      if (superseded(entry)) return
      const status = (err as { status?: number })?.status
      if (status === 409) errorMessage.value = 'Name already taken. Choose another.'
      else if (status === 404) errorMessage.value = 'Sprint not found.'
      else errorMessage.value = 'Failed to join. Please try again.'
      return
    }
    try {
      await runEntry('data')
    } finally {
      busy.value = false
    }
  }

  onMounted(() => runEntry('info'))

  onUnmounted(() => {
    currentEntry++
    participantStore.abandonPendingIdentity()
    cancelHub()
  })

  const showGoogleOption = computed(() => !authStore.isSignedIn)

  return {
    kicked,
    busy,
    errorMessage,
    joined,
    loading,
    roomError,
    refreshError,
    sprintName,
    currentPhase,
    summaryParticipants,
    isReportingPhase,
    isClosed,
    teamId,
    isScrumMaster,
    handleJoin,
    retryEntry,
    retryRefresh,
    reportStore,
    showGoogleOption,
  }
}

<template>
  <div class="min-h-screen bg-gray-50 py-8 px-4">
    <div v-if="reportStore.loading" class="flex items-center justify-center min-h-[60vh]">
      <p class="text-gray-500 text-sm">Loading report...</p>
    </div>

    <div v-else-if="reportStore.error" class="flex items-center justify-center min-h-[60vh]">
      <div class="bg-white rounded shadow p-8 text-center">
        <p class="text-red-600 text-sm">{{ reportStore.error }}</p>
      </div>
    </div>

    <div v-else-if="report" class="max-w-4xl mx-auto flex flex-col gap-6">
      <!-- Header -->
      <div class="bg-white rounded shadow p-6 flex items-start justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ report.sprint.name }} — Sprint Report</h1>
          <p class="text-sm text-gray-500 mt-1">
            <span class="font-medium text-gray-700">{{ report.team.name }}</span>
            &middot; {{ formatDate(report.sprint.createdAt) }}
          </p>
          <div class="flex flex-wrap gap-2 mt-3">
            <span
              v-for="p in report.participants"
              :key="p.displayName"
              class="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm"
            >
              {{ p.displayName }}
            </span>
          </div>
        </div>
        <button
          class="px-4 py-2 bg-gray-800 text-white rounded text-sm hover:bg-gray-900 shrink-0"
          @click="handleCopy"
        >
          Copy as markdown
        </button>
      </div>

      <!-- Went Well -->
      <div class="bg-white rounded shadow">
        <div class="px-4 py-3 border-b bg-green-50 rounded-t">
          <h2 class="font-semibold text-green-800 text-sm">Went Well</h2>
        </div>
        <div class="p-4">
          <div v-if="report.notes.wentWell.length === 0" class="text-gray-400 text-sm text-center py-4">
            No items
          </div>
          <ul v-else class="flex flex-col gap-3">
            <li v-for="(card, i) in report.notes.wentWell" :key="i">
              <p class="text-sm text-gray-800">{{ card.text }}</p>
              <div v-if="card.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <a
                  v-for="(link, j) in card.evidenceLinks"
                  :key="j"
                  :href="link.url"
                  target="_blank"
                  rel="noopener"
                  class="inline-block px-2 py-0.5 bg-green-100 text-green-700 rounded text-xs hover:bg-green-200"
                >
                  {{ link.title || truncateUrl(link.url) }}
                </a>
              </div>
            </li>
          </ul>
        </div>
      </div>

      <!-- Didn't Go Well -->
      <div class="bg-white rounded shadow">
        <div class="px-4 py-3 border-b bg-red-50 rounded-t">
          <h2 class="font-semibold text-red-800 text-sm">Didn't Go Well</h2>
        </div>
        <div class="p-4">
          <div v-if="report.notes.didntGoWell.length === 0" class="text-gray-400 text-sm text-center py-4">
            No items
          </div>
          <ul v-else class="flex flex-col gap-3">
            <li v-for="(card, i) in report.notes.didntGoWell" :key="i">
              <p class="text-sm text-gray-800">{{ card.text }}</p>
              <div v-if="card.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <a
                  v-for="(link, j) in card.evidenceLinks"
                  :key="j"
                  :href="link.url"
                  target="_blank"
                  rel="noopener"
                  class="inline-block px-2 py-0.5 bg-red-100 text-red-700 rounded text-xs hover:bg-red-200"
                >
                  {{ link.title || truncateUrl(link.url) }}
                </a>
              </div>
            </li>
          </ul>
        </div>
      </div>

      <!-- Action Items -->
      <div class="bg-white rounded shadow">
        <div class="px-4 py-3 border-b bg-blue-50 rounded-t">
          <h2 class="font-semibold text-blue-800 text-sm">Action Items</h2>
        </div>
        <div class="p-4">
          <div v-if="report.actionItems.length === 0" class="text-gray-400 text-sm text-center py-4">
            No action items
          </div>
          <ul v-else class="flex flex-col gap-3">
            <li v-for="(item, i) in report.actionItems" :key="i">
              <p class="text-sm text-gray-800">
                {{ item.text }}
                <span v-if="item.assigneeName" class="text-blue-600 font-medium ml-1">
                  @{{ item.assigneeName }}
                </span>
              </p>
              <div v-if="item.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <a
                  v-for="(link, j) in item.evidenceLinks"
                  :key="j"
                  :href="link.url"
                  target="_blank"
                  rel="noopener"
                  class="inline-block px-2 py-0.5 bg-blue-100 text-blue-700 rounded text-xs hover:bg-blue-200"
                >
                  {{ link.title || truncateUrl(link.url) }}
                </a>
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <p
      v-if="toastMessage"
      class="fixed bottom-4 right-4 bg-green-600 text-white px-4 py-2 rounded shadow text-sm"
    >
      {{ toastMessage }}
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useGuestStore } from '../stores/guest'
import { useReportStore } from '../stores/report'

const route = useRoute()
const guestStore = useGuestStore()
const reportStore = useReportStore()

const teamId = Number(route.params.teamId)
const sprintId = Number(route.params.sprintId)

const report = computed(() => reportStore.report)
const toastMessage = ref('')

let toastTimer: ReturnType<typeof setTimeout> | null = null
function showToast(msg: string) {
  toastMessage.value = msg
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => (toastMessage.value = ''), 3000)
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString()
}

function truncateUrl(url: string): string {
  try {
    const u = new URL(url)
    const path = u.pathname.length > 20 ? u.pathname.slice(0, 20) + '...' : u.pathname
    return u.hostname + path
  } catch {
    return url.length > 40 ? url.slice(0, 40) + '...' : url
  }
}

async function handleCopy() {
  const ok = await reportStore.copyAsMarkdown()
  if (ok) showToast('Copied to clipboard')
}

onMounted(() => {
  guestStore.loadToken(sprintId)
  reportStore.fetchReport(teamId, sprintId)
})
</script>

<template>
  <div class="max-w-4xl mx-auto flex flex-col gap-6">
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
        @click="$emit('copy-markdown')"
      >
        Copy as markdown
      </button>
    </div>

    <!-- Dynamic column sections -->
    <ReportSection
      v-for="col in report.columns"
      :key="col.columnName"
      :column-name="col.columnName"
      :column-color="col.columnColor"
      :groups="col.groups"
    />

    <!-- Action Items -->
    <div class="bg-white rounded shadow">
      <div class="px-4 py-3 border-b bg-col-blue-light rounded-t">
        <h2 class="font-semibold text-col-blue text-sm">Action Items</h2>
      </div>
      <div class="p-4">
        <div v-if="report.actionItems.length === 0" class="text-gray-400 text-sm text-center py-4">No action items</div>
        <ul v-else class="flex flex-col gap-3">
          <li v-for="(item, i) in report.actionItems" :key="i">
            <p class="text-sm text-gray-800">
              {{ item.text }}
              <span v-if="item.assigneeName" class="text-blue-600 font-medium ml-1">@{{ item.assigneeName }}</span>
            </p>
            <div v-if="item.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
              <a v-for="(link, j) in item.evidenceLinks" :key="j" :href="link.url" target="_blank" rel="noopener" class="inline-block px-2 py-0.5 bg-col-blue-light text-col-blue rounded text-xs hover:opacity-80">
                {{ link.title || truncateUrl(link.url) }}
              </a>
            </div>
          </li>
        </ul>
      </div>
    </div>

    <!-- Previous Actions Review -->
    <div v-if="report.previousActionsReview" class="bg-white rounded shadow">
      <div class="px-4 py-3 border-b bg-amber-50 rounded-t">
        <h2 class="font-semibold text-amber-800 text-sm">
          Previous Actions Review
          <span class="font-normal text-amber-600 ml-1">— Actions from: {{ report.previousActionsReview.previousSprintName }}</span>
        </h2>
      </div>
      <div class="p-4">
        <ul class="flex flex-col gap-3">
          <li v-for="(entry, i) in report.previousActionsReview.items" :key="i" class="flex items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <p class="text-sm text-gray-800">{{ entry.text }}</p>
              <p v-if="entry.assigneeName" class="text-xs text-gray-500 mt-0.5">@{{ entry.assigneeName }}</p>
            </div>
            <span :class="reviewStatusBadgeClass(entry.reviewStatus)" class="px-2 py-0.5 rounded text-xs font-medium shrink-0">
              {{ formatReviewStatus(entry.reviewStatus) }}
            </span>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ReportResponse } from '../../stores/report'
import ReportSection from './ReportSection.vue'

defineProps<{
  report: ReportResponse
}>()

defineEmits<{
  'copy-markdown': []
}>()

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

function reviewStatusBadgeClass(status: string): string {
  if (status === 'Done') return 'bg-emerald-100 text-emerald-700'
  if (status === 'CarriedOver') return 'bg-amber-100 text-amber-700'
  if (status === 'Dropped') return 'bg-gray-100 text-gray-600'
  return 'bg-red-50 text-red-600'
}

function formatReviewStatus(status: string): string {
  if (status === 'CarriedOver') return 'Carried Over'
  if (status === 'Not reviewed') return 'Not reviewed'
  return status
}
</script>

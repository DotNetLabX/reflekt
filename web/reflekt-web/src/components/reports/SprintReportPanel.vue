<template>
  <div class="max-w-4xl mx-auto flex flex-col gap-6">
    <BaseCard padding="lg">
      <div class="flex items-start justify-between">
        <div>
          <component :is="titleTag" class="text-2xl font-bold text-text-primary">
            {{ report.sprint.name }} — Sprint Report
          </component>
          <p class="text-sm text-text-muted mt-1">
            <span class="font-medium text-text-secondary">{{ report.team.name }}</span>
            &middot; {{ formatDate(report.sprint.createdAt) }}
          </p>
          <div class="flex flex-wrap gap-2 mt-3">
            <BaseBadge
              v-for="p in report.participants"
              :key="p.displayName"
              variant="primary"
              size="md"
            >
              {{ p.displayName }}
            </BaseBadge>
          </div>
        </div>
        <BaseButton variant="secondary" class="shrink-0" @click="$emit('copy-markdown')">
          Copy as markdown
        </BaseButton>
      </div>
    </BaseCard>

    <ReportSection
      v-for="col in report.columns"
      :key="col.columnName"
      :column-name="col.columnName"
      :column-color="col.columnColor"
      :groups="col.groups"
    />

    <BaseCard padding="none">
      <div class="px-4 py-3 border-b border-border bg-col-blue-light rounded-t-md">
        <h2 class="font-semibold text-col-blue text-sm">Action Items</h2>
      </div>
      <div class="p-4">
        <div
          v-if="report.actionItems.length === 0"
          class="text-text-muted text-sm text-center py-4"
        >
          No action items
        </div>
        <ul v-else class="flex flex-col gap-3">
          <li v-for="(item, i) in report.actionItems" :key="i">
            <p class="text-sm text-text-primary">
              {{ item.text }}
              <span v-if="item.assigneeName" class="text-primary-600 font-medium ml-1"
                >@{{ item.assigneeName }}</span
              >
            </p>
            <div v-if="item.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
              <EvidenceChip
                v-for="(link, j) in item.evidenceLinks"
                :key="j"
                :link="link"
                color="blue"
              />
            </div>
          </li>
        </ul>
      </div>
    </BaseCard>

    <BaseCard v-if="report.previousActionsReview" padding="none">
      <div class="px-4 py-3 border-b border-border bg-badge-warning-bg rounded-t-md">
        <h2 class="font-semibold text-badge-warning-text text-sm">
          Previous Actions Review
          <span class="font-normal text-text-muted ml-1"
            >— Actions from: {{ report.previousActionsReview.previousSprintName }}</span
          >
        </h2>
      </div>
      <div class="p-4">
        <ul class="flex flex-col gap-3">
          <li
            v-for="(entry, i) in report.previousActionsReview.items"
            :key="i"
            class="flex items-start justify-between gap-4"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm text-text-primary">{{ entry.text }}</p>
              <p v-if="entry.assigneeName" class="text-xs text-text-muted mt-0.5">
                @{{ entry.assigneeName }}
              </p>
            </div>
            <BaseBadge :variant="REVIEW_STATUS_BADGE[entry.reviewStatus]" class="shrink-0">
              {{ formatStatus(entry.reviewStatus) }}
            </BaseBadge>
          </li>
        </ul>
      </div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
import BaseBadge from '@/components/ui/BaseBadge.vue'
import BaseButton from '@/components/ui/BaseButton.vue'
import BaseCard from '@/components/ui/BaseCard.vue'
import { REVIEW_STATUS_BADGE } from '@/constants/badges'
import { formatDate, formatStatus } from '@/format/format'
import type { ReportResponse } from '@/types/report'
import ReportSection from './ReportSection.vue'
import EvidenceChip from './EvidenceChip.vue'

withDefaults(
  defineProps<{
    report: ReportResponse
    titleTag?: 'h1' | 'h2'
  }>(),
  { titleTag: 'h1' },
)

defineEmits<{
  'copy-markdown': []
}>()
</script>

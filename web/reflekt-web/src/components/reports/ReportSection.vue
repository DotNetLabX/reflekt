<template>
  <BaseCard padding="none">
    <div
      class="px-4 py-3 border-b border-border rounded-t-md"
      :class="`bg-col-${columnColor}-light`"
    >
      <h2 class="font-semibold text-sm" :class="`text-col-${columnColor}`">{{ columnName }}</h2>
    </div>
    <div class="p-4">
      <div v-if="groups.length === 0" class="text-text-muted text-sm text-center py-4">
        No items
      </div>
      <div v-else class="flex flex-col gap-4">
        <div v-for="(group, i) in groups" :key="i">
          <div class="flex items-center gap-2 mb-1">
            <p class="text-sm font-medium text-text-primary">{{ group.groupTitle }}</p>
            <span class="text-xs text-text-muted">({{ group.voteCount }} votes)</span>
            <BaseBadge v-if="group.isDiscussed" variant="success">discussed</BaseBadge>
          </div>
          <ul class="flex flex-col gap-1 pl-3 border-l-2" :class="`border-col-${columnColor}`">
            <li v-for="(card, j) in group.cards" :key="j">
              <p class="text-sm text-text-secondary">{{ card.text }}</p>
              <div v-if="card.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <EvidenceChip
                  v-for="(link, k) in card.evidenceLinks"
                  :key="k"
                  :link="link"
                  :color="columnColor"
                />
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import BaseBadge from '@/components/ui/BaseBadge.vue'
import BaseCard from '@/components/ui/BaseCard.vue'
import EvidenceChip from './EvidenceChip.vue'
import type { ReportGroupEntry } from '@/types/report'
import type { ColumnColor } from '@/types/domain'

defineProps<{
  columnName: string
  columnColor: ColumnColor
  groups: ReportGroupEntry[]
}>()
</script>

<template>
  <div :class="cardClasses">
    <div v-if="$slots.header" class="border-b border-border px-4 py-3">
      <slot name="header" />
    </div>
    <div :class="bodyPadding">
      <slot />
    </div>
    <div v-if="$slots.footer" class="border-t border-border px-4 py-3">
      <slot name="footer" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    padding?: 'none' | 'sm' | 'md' | 'lg'
    hoverable?: boolean
    radius?: 'sm' | 'md' | 'lg'
  }>(),
  {
    padding: 'md',
    hoverable: false,
    radius: 'md',
  },
)

const cardClasses = computed(() => {
  const base = 'bg-surface border border-border shadow-card'
  const radii = { sm: 'rounded-sm', md: 'rounded-md', lg: 'rounded-lg' }
  const hover = props.hoverable
    ? 'transition-all hover:shadow-card-hover hover:-translate-y-0.5'
    : ''
  return [base, radii[props.radius], hover]
})

const bodyPadding = computed(() => {
  const paddings = {
    none: '',
    sm: 'p-3',
    md: 'p-4',
    lg: 'p-6',
  }
  return paddings[props.padding]
})
</script>

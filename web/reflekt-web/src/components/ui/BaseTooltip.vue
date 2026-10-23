<template>
  <div class="relative group/tooltip">
    <slot :describedby="disabled ? undefined : tooltipId" />
    <div
      v-if="!disabled"
      :id="tooltipId"
      role="tooltip"
      class="absolute bg-surface-raised text-text-primary border border-border text-xs opacity-0 group-hover/tooltip:opacity-100 group-focus-within/tooltip:opacity-100 transition-opacity pointer-events-none"
      :class="[variantClasses[variant], panelClass]"
    >
      <slot name="content">{{ text }}</slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useId } from 'vue'

withDefaults(
  defineProps<{
    text?: string
    panelClass?: string
    variant?: 'text' | 'panel'
    disabled?: boolean
  }>(),
  {
    text: '',
    panelClass: '',
    variant: 'text',
    disabled: false,
  },
)

const variantClasses = {
  text: 'rounded px-2 py-1 z-10',
  panel: 'rounded-sm p-2 shadow-lg z-20',
}

const tooltipId = useId()
</script>

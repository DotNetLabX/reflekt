<template>
  <RouterLink v-if="to" :to="to" :class="buttonClasses">
    <slot name="icon" />
    <slot />
  </RouterLink>
  <button v-else :class="buttonClasses" :disabled="disabled || loading">
    <LoaderCircle v-if="loading" class="animate-spin shrink-0" :size="iconSize" />
    <slot name="icon" v-if="!loading" />
    <slot />
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { LoaderCircle } from 'lucide-vue-next'
import type { RouteLocationRaw } from 'vue-router'

const props = withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'success' | 'danger' | 'ghost'
    size?: 'sm' | 'md' | 'lg'
    loading?: boolean
    disabled?: boolean
    to?: RouteLocationRaw
  }>(),
  {
    variant: 'primary',
    size: 'md',
    loading: false,
    disabled: false,
  },
)

const iconSize = computed(() => {
  const sizes = { sm: 14, md: 16, lg: 18 }
  return sizes[props.size]
})

const buttonClasses = computed(() => {
  const base =
    'inline-flex items-center justify-center gap-2 font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary-500 ring-offset-focus-offset'

  const variants = {
    primary: 'bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800',
    secondary:
      'border border-border bg-surface text-text-secondary hover:bg-surface-raised active:bg-surface-raised',
    success: 'bg-success-solid text-white hover:bg-success-hover active:bg-success-hover',
    danger: 'bg-danger text-white hover:bg-danger-hover active:bg-danger-active',
    ghost: 'text-text-secondary hover:bg-surface-raised active:bg-surface-raised',
  }

  const sizes = {
    sm: 'px-2.5 py-1.5 text-xs rounded-sm',
    md: 'px-4 py-2 text-sm rounded-md',
    lg: 'px-5 py-2.5 text-base rounded-md',
  }

  const disabled =
    props.disabled || props.loading
      ? 'opacity-50 cursor-not-allowed pointer-events-none'
      : 'cursor-pointer'

  return [base, variants[props.variant], sizes[props.size], disabled]
})
</script>

<template>
  <button
    :class="buttonClasses"
    :disabled="disabled || loading"
    v-bind="$attrs"
  >
    <LoaderCircle v-if="loading" class="animate-spin shrink-0" :size="iconSize" />
    <slot name="icon" v-if="!loading" />
    <slot />
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { LoaderCircle } from 'lucide-vue-next';

const props = withDefaults(defineProps<{
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  disabled?: boolean;
}>(), {
  variant: 'primary',
  size: 'md',
  loading: false,
  disabled: false,
});

const iconSize = computed(() => {
  const sizes = { sm: 14, md: 16, lg: 18 };
  return sizes[props.size];
});

const buttonClasses = computed(() => {
  const base = 'inline-flex items-center justify-center gap-2 font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-primary-500';

  const variants = {
    primary: 'bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800',
    secondary: 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 active:bg-gray-100',
    danger: 'bg-danger text-white hover:bg-red-600 active:bg-red-700',
    ghost: 'text-gray-700 hover:bg-gray-100 active:bg-gray-200',
  };

  const sizes = {
    sm: 'px-2.5 py-1.5 text-xs rounded-[--radius-sm]',
    md: 'px-4 py-2 text-sm rounded-[--radius-md]',
    lg: 'px-5 py-2.5 text-base rounded-[--radius-md]',
  };

  const disabled = (props.disabled || props.loading) ? 'opacity-50 cursor-not-allowed pointer-events-none' : 'cursor-pointer';

  return [base, variants[props.variant], sizes[props.size], disabled];
});
</script>

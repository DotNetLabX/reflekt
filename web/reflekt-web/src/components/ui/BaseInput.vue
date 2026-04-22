<template>
  <div>
    <input
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :maxlength="maxlength"
      :disabled="disabled"
      :class="inputClasses"
      v-bind="$attrs"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" class="mt-1 text-sm text-danger">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  modelValue?: string | number;
  type?: string;
  placeholder?: string;
  maxlength?: number;
  disabled?: boolean;
  error?: string;
}>(), {
  type: 'text',
  disabled: false,
});

defineEmits<{
  'update:modelValue': [value: string];
}>();

const inputClasses = computed(() => {
  const base = 'block w-full px-3 py-2 text-sm border rounded-[--radius-md] transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500';

  if (props.disabled) {
    return [base, 'bg-gray-50 text-gray-400 cursor-not-allowed border-gray-200'];
  }
  if (props.error) {
    return [base, 'border-danger text-gray-900 focus:ring-danger focus:border-danger'];
  }
  return [base, 'border-gray-300 text-gray-900 bg-white'];
});
</script>

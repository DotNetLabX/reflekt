<template>
  <div :class="attrs.class" :style="attrs.style as StyleValue">
    <label v-if="label" :for="inputId" class="block text-sm font-medium text-text-secondary mb-1">
      {{ label }}
    </label>
    <input
      :id="inputId"
      :type="type"
      :value="model"
      :placeholder="placeholder"
      :maxlength="maxlength"
      :disabled="disabled"
      :class="inputClasses"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="error ? errorId : undefined"
      v-bind="inputAttrs()"
      @input="model = ($event.target as HTMLInputElement).value"
    />
    <p v-if="error" :id="errorId" class="mt-1 text-sm text-danger">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed, useAttrs, useId, type StyleValue } from 'vue'

defineOptions({ inheritAttrs: false })

const attrs = useAttrs()

// attrs is not reactive, so this is called from the render, never wrapped in computed().
function inputAttrs() {
  return Object.fromEntries(
    Object.entries(attrs).filter(([key]) => key !== 'class' && key !== 'style'),
  )
}

const model = defineModel<string | number>()

const props = withDefaults(
  defineProps<{
    id?: string
    label?: string
    type?: string
    placeholder?: string
    maxlength?: number
    disabled?: boolean
    error?: string
  }>(),
  {
    type: 'text',
    disabled: false,
  },
)

const generatedId = useId()
const inputId = computed(() => props.id ?? generatedId)
const errorId = computed(() => `${inputId.value}-error`)

const inputClasses = computed(() => {
  const base =
    'block w-full px-3 py-2 text-sm border rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ring-offset-focus-offset'

  if (props.disabled) {
    return [base, 'bg-surface-raised text-text-muted cursor-not-allowed border-border']
  }
  if (props.error) {
    return [base, 'border-danger text-text-primary focus:ring-danger focus:border-danger']
  }
  return [base, 'border-border text-text-primary bg-surface']
})
</script>

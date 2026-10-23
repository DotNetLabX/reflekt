<template>
  <div class="flex items-center gap-2">
    <BaseInput
      v-model="name"
      :placeholder="`Column ${index} name`"
      :aria-label="`Column ${index} name`"
      :maxlength="30"
      class="flex-1"
      @input="$emit('edited', 'name')"
    />
    <div class="flex gap-1">
      <button
        v-for="swatch in PALETTE_COLORS"
        :key="swatch"
        type="button"
        :title="swatch"
        :aria-label="`Column ${index} colour: ${swatch}`"
        :aria-pressed="color === swatch"
        class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
        :class="[
          `bg-col-${swatch}`,
          color === swatch ? 'border-text-primary scale-110' : 'border-transparent',
        ]"
        @click="pickColor(swatch)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseInput from '@/components/ui/BaseInput.vue'
import { PALETTE_COLORS } from '@/constants/columns'
import type { ColumnColor } from '@/types/domain'

defineProps<{ index: number }>()

const name = defineModel<string>('name', { required: true })
const color = defineModel<ColumnColor>('color', { required: true })

const emit = defineEmits<{ edited: [field: 'name' | 'color'] }>()

function pickColor(swatch: ColumnColor) {
  color.value = swatch
  emit('edited', 'color')
}
</script>

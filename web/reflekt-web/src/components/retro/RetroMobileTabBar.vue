<template>
  <div class="flex sm:hidden gap-1">
    <button
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'WentWell' ? 'bg-went-well text-white' : 'bg-went-well-light text-went-well'"
      @click="$emit('update:modelValue', 'WentWell')"
    >
      Went Well
    </button>
    <button
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'DidntGoWell' ? 'bg-didnt-go-well text-white' : 'bg-didnt-go-well-light text-didnt-go-well'"
      @click="$emit('update:modelValue', 'DidntGoWell')"
    >
      Didn't Go Well
    </button>
    <button
      v-if="showActionItems"
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'ActionItems' ? 'bg-action-items text-white' : 'bg-action-items-light text-action-items'"
      @click="$emit('update:modelValue', 'ActionItems')"
    >
      Actions
    </button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: 'WentWell' | 'DidntGoWell' | 'ActionItems'
  showActionItems: boolean
}>()

defineEmits<{
  'update:modelValue': [value: 'WentWell' | 'DidntGoWell' | 'ActionItems']
}>()
</script>

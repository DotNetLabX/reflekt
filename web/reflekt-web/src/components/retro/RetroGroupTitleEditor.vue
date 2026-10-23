<template>
  <div class="flex gap-1">
    <input
      v-model="title"
      maxlength="100"
      placeholder="Group title..."
      aria-label="Group title"
      class="flex-1 border border-border rounded-sm px-2 py-1 text-xs text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
      @keydown.enter="$emit('save')"
      @keydown.escape="$emit('cancel')"
    />
    <button
      class="px-2 py-1 text-xs bg-primary-600 text-white rounded-sm cursor-pointer"
      @click="$emit('save')"
    >
      Save
    </button>
  </div>
</template>

<script setup lang="ts">
const title = defineModel<string>({ required: true })

defineEmits<{
  save: []
  cancel: []
}>()
</script>

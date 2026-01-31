<template>
  <transition name="fade">
    <div v-if="visible" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-30 dark:bg-opacity-50">
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-8 max-w-sm w-full flex flex-col items-center">
        <div class="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-4">
          {{ message }}
        </div>
        <div class="flex space-x-4 mt-2">
          <button @click="confirm" class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg">Yes</button>
          <button @click="cancel" class="bg-gray-300 dark:bg-gray-600 hover:bg-gray-400 dark:hover:bg-gray-700 text-gray-800 dark:text-gray-100 px-4 py-2 rounded-lg">No</button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { ref, defineProps, defineEmits, watch } from 'vue'
const props = defineProps<{ modelValue: boolean, message: string }>()
const emit = defineEmits(['update:modelValue', 'confirm', 'cancel'])
const visible = ref(props.modelValue)

watch(() => props.modelValue, (val) => {
  visible.value = val
})

function confirm() {
  emit('confirm')
  emit('update:modelValue', false)
}
function cancel() {
  emit('cancel')
  emit('update:modelValue', false)
}
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity .2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>

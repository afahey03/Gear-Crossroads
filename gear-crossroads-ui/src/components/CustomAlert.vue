<template>
  <transition name="fade">
  <div v-if="visible" class="fixed top-20 left-1/2 transform -translate-x-1/2 bg-white dark:bg-gray-800 border border-blue-400 dark:border-blue-500 shadow-lg px-6 py-3 rounded-xl z-50 flex items-center space-x-3">
      <span class="text-blue-600 dark:text-blue-400 font-semibold">{{ message }}</span>
      <button @click="close" class="ml-4 text-gray-400 dark:text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">&times;</button>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { ref, watch, defineProps, defineEmits } from 'vue'

const props = defineProps<{ message: string, duration?: number }>()
const emit = defineEmits(['close'])
const visible = ref(true)

watch(() => props.message, () => {
  visible.value = true
  if (props.duration !== 0) {
    setTimeout(close, props.duration || 2500)
  }
})

function close() {
  visible.value = false
  emit('close')
}
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>

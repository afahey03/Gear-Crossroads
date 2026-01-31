
<template>
  <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-lg dark:shadow-gray-900 p-6 hover:shadow-xl dark:hover:shadow-gray-900 transform hover:-translate-y-1 transition">
    <div v-if="imageUrl" class="flex justify-center mb-3">
      <img 
        :src="imageUrl" 
        alt="Setup image" 
        class="max-h-40 object-contain rounded-lg"
        @error="onImageError"
      />
    </div>
    <h2 class="text-xl font-semibold mb-2 text-gray-900 dark:text-gray-100">{{ title }}</h2>
    <p class="text-gray-600 dark:text-gray-400 mb-4">{{ description }}</p>
    <router-link
      v-if="setupId !== undefined"
      :to="{ path: `/setups/${setupId}`, query: linkQuery || {} }"
      class="bg-blue-500 dark:bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-600 dark:hover:bg-blue-700 transition flex items-center justify-center"
    >
      View Setup
    </router-link>
  </div>
</template>

<script lang="ts">
import type { PropType } from 'vue';

export default {
  props: {
    title: { type: String as PropType<string>, required: true },
    description: { type: String as PropType<string>, required: true },
    imageUrl: { type: String as PropType<string>, required: false },
    setupId: { type: [String, Number] as PropType<string | number>, required: true },
    linkQuery: { type: Object as PropType<Record<string, any>>, required: false }
  },
  methods: {
    onImageError(e: Event) {
      // Hide the image if it fails to load
      const img = e.target as HTMLImageElement;
      if (img.parentElement) {
        img.parentElement.style.display = 'none';
      }
    }
  }
};
</script>

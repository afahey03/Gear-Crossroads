<template>
  <div class="bg-white rounded-2xl shadow-lg p-6 hover:shadow-xl transform hover:-translate-y-1 transition">
    <h2 class="text-xl font-semibold mb-2">{{ title }}</h2>
    <p class="text-gray-600 mb-4">{{ description }}</p>
    <button class="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600 transition">
      View Setup
    </button>
  </div>
</template>

<script lang="ts">
import type { PropType } from 'vue';

export default {
  props: {
    title: { type: String as PropType<string>, required: true },
    description: { type: String as PropType<string>, required: true }
  }
};
</script>

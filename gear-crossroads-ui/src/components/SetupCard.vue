<template>
  <div class="setup-card">
    <h2>{{ title }}</h2>
    <p>{{ description }}</p>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';

export default defineComponent({
  props: {
    title: { type: String as PropType<string>, required: true },
    description: { type: String as PropType<string>, required: true },
  },
});
</script>

<style scoped>
.setup-card {
  border: 1px solid #ccc;
  padding: 1rem;
  margin-bottom: 1rem;
  border-radius: 8px;
}
</style>

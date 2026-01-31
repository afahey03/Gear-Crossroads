<template>
  <div>
    <section class="bg-gradient-to-r from-blue-600 to-blue-500 dark:from-blue-700 dark:to-blue-600 text-white py-24 px-6 text-center">
      <h1 class="text-4xl md:text-5xl font-bold mb-4">Discover the Gear People Love</h1>
      <p class="text-lg md:text-xl mb-8">Explore setups from other enthusiasts, get inspired, and share your own!</p>
      <router-link
        :to="userStore.token ? '/setups' : '/register'"
        class="bg-white dark:bg-gray-800 text-blue-600 dark:text-blue-400 font-semibold px-6 py-3 rounded-lg shadow hover:bg-gray-100 dark:hover:bg-gray-700 transition"
      >
        Get Started
      </router-link>
    </section>

    <section class="max-w-7xl mx-auto py-12 px-6">
      <h2 class="text-3xl font-bold mb-8 text-center text-gray-900 dark:text-gray-100">Popular Setups</h2>
      <div v-if="loadingPopular" class="text-center text-gray-500 dark:text-gray-400">Loading...</div>
      <div v-else-if="!popular.length" class="text-center text-gray-500 dark:text-gray-400">No setups yet.</div>
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
        <SetupCard
          v-for="s in popular"
          :key="s.id"
          :title="s.title"
          :description="s.description || ''"
          :image-url="resolveImageUrl(s.imageUrl)"
          :setup-id="s.id"
          :link-query="{ from: 'home' }"
        />
      </div>
    </section>
  </div>
</template>

<script lang="ts">
import SetupCard from '../components/SetupCard.vue';
import { useUserStore } from '../stores/user';
import { ref, onMounted } from 'vue';
import axios from 'axios';
export default {
  components: { SetupCard },
  setup() {
    const userStore = useUserStore();
  const popular = ref<any[]>([]);
  const loadingPopular = ref(false);
    const resolveImageUrl = (url?: string): string => {
      if (!url) return '';
      if (url.startsWith('http://') || url.startsWith('https://')) return url;
      const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com';
      return `${baseUrl}/${url.replace(/^\/+/, '')}`;
    };
    const loadPopular = async () => {
      loadingPopular.value = true;
      try {
        const headers: any = {};
        if (userStore.token) headers.Authorization = `Bearer ${userStore.token}`;
        const res = await axios.get('/api/setups/popular', { headers });
        popular.value = Array.isArray(res.data) ? res.data : [];
      } finally {
        loadingPopular.value = false;
      }
    };
    onMounted(loadPopular);
    return { userStore, popular, loadingPopular, resolveImageUrl };
  }
};
</script>

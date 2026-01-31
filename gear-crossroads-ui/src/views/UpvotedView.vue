<template>
  <div class="pt-20">
    <div class="max-w-6xl mx-auto p-6">
      <h1 class="text-3xl font-bold mb-6 text-gray-900 dark:text-gray-100">Your Upvoted Setups</h1>
      <div class="flex flex-col md:flex-row gap-4 mb-6">
        <div class="flex-1">
          <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Category</label>
          <select v-model="selectedCategory" class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100">
            <option value="">All categories</option>
            <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
          </select>
        </div>
        <button @click="loadList" class="self-end md:self-auto bg-blue-600 dark:bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-700 dark:hover:bg-blue-600">Apply</button>
      </div>

      <div v-if="loading" class="text-gray-500 dark:text-gray-400">Loading...</div>
      <div v-else-if="!items.length" class="text-gray-500 dark:text-gray-400">No upvoted setups yet.</div>
      <div v-else class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div v-for="s in items" :key="s.id" class="bg-white dark:bg-gray-800 rounded-xl shadow dark:shadow-gray-900 p-4">
          <router-link :to="{ path: `/setups/${s.id}`, query: { from: 'upvoted' } }" class="font-semibold text-lg mb-2 hover:underline block text-gray-900 dark:text-gray-100">{{ s.title }}</router-link>
          <div v-if="s.imageUrl" class="mb-2 flex justify-center">
            <img 
              :src="resolveImageUrl(s.imageUrl)" 
              class="h-40 object-contain rounded-lg"
              alt="Setup image"
              @error="(e) => (e.target as HTMLElement).style.display = 'none'"
            />
          </div>
          <p class="text-gray-600 dark:text-gray-400 mb-2">{{ s.description }}</p>
          <div class="flex items-center justify-between">
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ s.category }}</span>
            <button @click="toggleUpvote(s)" class="text-sm px-3 py-1 rounded border border-gray-300 dark:border-gray-600" :class="{ 'bg-blue-600 dark:bg-blue-500 text-white': s.hasVoted, 'text-gray-700 dark:text-gray-300': !s.hasVoted }">
              ▲ {{ s.voteCount || 0 }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { useUserStore } from '../stores/user'

const userStore = useUserStore()
const items = ref<any[]>([])
const loading = ref(true)
const categories = ref<string[]>(["Photography","Gaming","Climbing","Music","Streaming","Magic: The Gathering","Disc Golf","Fishing/Tackle","Podcasting","Woodworking","Cooking","Cycling","Running","Art","Work/Office","Home/Desk","Other"]) // fallback
const selectedCategory = ref<string>('')

const resolveImageUrl = (url: string | null | undefined): string => {
  if (!url) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com'
  return `${baseUrl}/${url.replace(/^\/+/, '')}`
}

const loadCategories = async () => {
  try {
    const res = await axios.get('/api/setups/categories')
    if (Array.isArray(res.data) && res.data.length) categories.value = res.data
  } catch {}
}

const loadList = async () => {
  loading.value = true
  try {
    const params: any = {}
    if (selectedCategory.value) params.category = selectedCategory.value
    const headers: any = { Authorization: `Bearer ${userStore.token}` }
    const res = await axios.get('/api/setups/upvoted', { params, headers })
    items.value = res.data
  } finally {
    loading.value = false
  }
}

const toggleUpvote = async (s: any) => {
  if (!userStore.token) return
  try {
    const res = await axios.post(`/api/setups/${s.id}/upvote`, {}, {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    s.hasVoted = res.data.voted
    s.voteCount = res.data.voteCount
    // If the user un-upvotes here, remove from the list
    if (!s.hasVoted) {
      items.value = items.value.filter(i => i.id !== s.id)
    }
  } catch {}
}

onMounted(async () => {
  await loadCategories()
  await loadList()
})
</script>

<style scoped>
.line-clamp-3 {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-clamp: 3;
}
</style>

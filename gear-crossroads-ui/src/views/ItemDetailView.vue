<template>
  <div class="pt-20">
    <div class="max-w-3xl mx-auto mt-10 bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-md dark:shadow-gray-900">
      <div v-if="item">
        <div class="flex flex-col md:flex-row gap-6">
          <div class="flex-1">
            <div class="flex items-start justify-between gap-4">
              <h1 class="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">{{ item?.name }}</h1>
              <button
                v-if="canEdit && !editMode"
                class="h-10 px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded hover:bg-blue-700 dark:hover:bg-blue-600"
                @click="editMode = true"
              >
                Edit
              </button>
            </div>
            <p v-if="item.description" class="mb-2 text-gray-700 dark:text-gray-300">
              <span class="font-semibold">Description:</span> {{ item.description }}
            </p>
            
            <div class="flex gap-4 mt-4">
              <!-- Primary back to Setup when we have setup context -->
              <router-link
                v-if="item?.setupItems?.length"
                :to="{ name: 'SetupDetail', params: { id: item.setupItems[0].setupId }, query: $route.query }"
                class="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Back to Setup
              </router-link>
              <!-- Origin specific links (siblings, not nested) -->
              <router-link
                v-if="$route.query.from === 'feed'"
                :to="{ name: 'feed', query: { category: $route.query.category, maxAgeDays: $route.query.maxAgeDays, minAgeDays: $route.query.minAgeDays } }"
                class="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Back to Feed
              </router-link>
              <router-link
                v-if="$route.query.from === 'upvoted'"
                :to="{ name: 'upvoted' }"
                class="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Back to Upvoted
              </router-link>
              <router-link
                v-if="$route.query.from === 'home'"
                :to="{ name: 'Home' }"
                class="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Back to Home
              </router-link>
              <router-link
                v-if="!$route.query.from || ($route.query.from !== 'feed' && $route.query.from !== 'upvoted' && $route.query.from !== 'home')"
                :to="{ name: 'Setups' }"
                class="text-blue-600 dark:text-blue-400 hover:underline"
              >
                Your Setups
              </router-link>
            </div>
          </div>
          <div class="w-full md:w-72">
            <div v-if="item.imageUrl" class="mb-4 flex justify-center">
              <img 
                :src="item.imageUrl" 
                alt="Item image" 
                class="max-h-64 rounded-lg border border-gray-300 dark:border-gray-600 object-contain"
                @error="handleImageError"
                loading="lazy"
              />
            </div>
            <div v-if="canEdit && editMode" class="border-t border-gray-200 dark:border-gray-700 pt-4">
              <h3 class="font-semibold mb-2 text-gray-900 dark:text-gray-100">Update image</h3>
              <form @submit.prevent="uploadItemImage" class="flex flex-col gap-2">
                <input type="file" accept="image/*" @change="onFileChange" class="bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded p-2" />
                <button
                  type="submit"
                  class="bg-blue-600 dark:bg-blue-500 text-white px-3 py-2 rounded disabled:opacity-50 hover:bg-blue-700 dark:hover:bg-blue-600"
                  :disabled="!imageFile || uploading"
                >
                  {{ uploading ? 'Uploading...' : 'Upload Image' }}
                </button>
              </form>
              <button
                v-if="item.imageUrl"
                @click="deleteItemImage"
                class="mt-3 bg-red-600 text-white px-3 py-2 rounded hover:bg-red-700"
              >
                Delete Image
              </button>
            </div>
          </div>
        </div>
  <div v-if="canEdit && editMode" class="mt-8 border-t border-gray-200 dark:border-gray-700 pt-6">
          <h2 class="text-xl font-semibold mb-3 text-gray-900 dark:text-gray-100">Edit item</h2>
          <form @submit.prevent="saveItemEdit" class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <input v-model="editName" class="border border-gray-300 dark:border-gray-600 rounded p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" placeholder="Name" />
            <input v-model="editDescription" class="border border-gray-300 dark:border-gray-600 rounded p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" placeholder="Description" />
            <div class="md:col-span-3 flex gap-2">
              <button type="submit" class="bg-green-600 dark:bg-green-500 text-white px-4 py-2 rounded hover:bg-green-700 dark:hover:bg-green-600">Save</button>
              <button type="button" @click="cancelEdits" class="bg-gray-200 dark:bg-gray-600 text-gray-800 dark:text-gray-100 px-4 py-2 rounded hover:bg-gray-300 dark:hover:bg-gray-700">Cancel</button>
              <button type="button" @click="deleteItem" class="ml-auto bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700">
                Delete Item
              </button>
            </div>
          </form>
        </div>
      </div>
      <div v-else class="text-gray-500 dark:text-gray-400">Loading...</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'

const route = useRoute()
const item = ref<any>(null)
const router = useRouter()
const userStore = useUserStore()
const alertStore = useAlertStore?.()
const editMode = ref(false)

const backendBaseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com';

const fixImageUrl = (url: string | null | undefined, bustCache: boolean = false): string => {
  if (!url) return '';
  // Already a full URL
  if (url.startsWith('http://') || url.startsWith('https://')) {
    console.log('[fixImageUrl] Already full URL:', url);
    return url;
  }
  // Relative URL - prepend base URL
  const cleanUrl = url.replace(/^\/+/, ''); // Remove leading slashes
  const fullUrl = `${backendBaseUrl}/${cleanUrl}`;
  
  console.log('[fixImageUrl] Converting:', url, '->', fullUrl);
  
  // Optionally add cache-busting parameter (e.g., after upload)
  if (bustCache) {
    const finalUrl = `${fullUrl}?t=${Date.now()}`;
    console.log('[fixImageUrl] With cache-bust:', finalUrl);
    return finalUrl;
  }
  return fullUrl;
}

const handleImageError = (e: Event) => {
  const img = e.target as HTMLImageElement
  console.warn('[Image Error] Failed to load image:', img.src)
  // Hide the parent container
  if (img.parentElement) {
    img.parentElement.style.display = 'none'
  }
}

const loadItem = async (bustCache: boolean = false) => {
  const res = await axios.get(`/api/items/${route.params.id}`)
  const data = res.data
  data.imageUrl = fixImageUrl(data.imageUrl, bustCache)
  item.value = data
  editName.value = item.value?.name || ''
  editDescription.value = item.value?.description || ''
  
  if (!item.value.setupItems) {
  }
}

onMounted(loadItem)

const canEdit = computed(() => !!item.value?.canEdit)

const editName = ref('')
const editDescription = ref('')
 

const resetEdits = () => {
  editName.value = item.value?.name || ''
  editDescription.value = item.value?.description || ''
  
}

const cancelEdits = () => {
  resetEdits()
  editMode.value = false
}

const saveItemEdit = async () => {
  if (!item.value) return
  try {
    await axios.put(
      `/api/items/${item.value.id}`,
      {
        name: editName.value,
        description: editDescription.value,
      },
      { headers: { Authorization: `Bearer ${userStore.token}` } }
    )
    await loadItem()
    alertStore?.show?.('Item updated', 'success')
  } catch (err) {
    console.error(err)
    alertStore?.show?.('Failed to update item', 'error')
  }
}

const imageFile = ref<File | null>(null)
const uploading = ref(false)
const onFileChange = (e: Event) => {
  const target = e.target as HTMLInputElement
  if (target.files && target.files[0]) {
    const file = target.files[0]
    const maxSize = 10 * 1024 * 1024 // 10MB in bytes
    
    if (file.size > maxSize) {
      alertStore.show('Image file size must be less than 10MB.', 'error')
      target.value = '' // Clear the file input
      imageFile.value = null
      return
    }
    
    imageFile.value = file
  } else {
    imageFile.value = null
  }
}

const uploadItemImage = async () => {
  if (!item.value || !imageFile.value) return
  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('Image', imageFile.value)
    await axios.put(`/api/items/${item.value.id}/image`, formData, {
      headers: {
        Authorization: `Bearer ${userStore.token}`,
        'Content-Type': 'multipart/form-data',
      },
    })
    imageFile.value = null
    // Bust cache to ensure new image is loaded
    await loadItem(true)
    alertStore?.show?.('Image uploaded', 'success')
  } catch (err: any) {
    console.error(err)
    const errorMessage = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.title || err.response?.data?.message || 'Failed to upload image'
    alertStore?.show?.(errorMessage, 'error')
  } finally {
    uploading.value = false
  }
}

const deleteItemImage = async () => {
  if (!item.value) return
  try {
    await axios.delete(`/api/items/${item.value.id}/image`, {
      headers: { Authorization: `Bearer ${userStore.token}` },
    })
    await loadItem()
    alertStore?.show?.('Image removed', 'success')
  } catch (err) {
    console.error(err)
    alertStore?.show?.('Failed to remove image', 'error')
  }
}

const deleteItem = async () => {
  if (!item.value) return
  try {
    await axios.delete(`/api/items/${item.value.id}`, {
      headers: { Authorization: `Bearer ${userStore.token}` },
    })
    alertStore?.show?.('Item deleted', 'success')

    // Determine setup context if available
    const firstSetupId: number | undefined = item.value?.setupItems?.length ? item.value.setupItems[0].setupId : undefined
    const from = route.query.from as string | undefined

    // If we have a setup id, prefer navigating back to that setup detail, preserving origin query state
    if (firstSetupId) {
      router.replace({ name: 'SetupDetail', params: { id: firstSetupId }, query: route.query })
      return
    }

    // Fallback to origin-specific broader page if no direct setup context
    if (from === 'feed') {
      router.replace({ name: 'feed', query: { category: route.query.category, maxAgeDays: route.query.maxAgeDays, minAgeDays: route.query.minAgeDays } })
    } else if (from === 'upvoted') {
      router.replace({ name: 'upvoted' })
    } else if (from === 'home') {
      router.replace({ name: 'Home' })
    } else {
      router.replace({ name: 'Setups' })
    }
  } catch (err) {
    console.error(err)
    alertStore?.show?.('Failed to delete item', 'error')
  }
}
</script>

<style scoped>
img {
  display: block;
  margin: 0 auto;
}
</style>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import CommentSection from '../components/CommentSection.vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const alertStore = useAlertStore()

const showConfirmDialog = ref(false)
const confirmMessage = ref('')
const editMode = ref(false)
const editTitle = ref('')
const editDescription = ref('')

const setup = ref<any>(null)
const setupItems = ref<any[]>([])

const newItemName = ref('')
const newItemDescription = ref('')
const newItemImageFile = ref<File | null>(null)
const newItemImagePreview = ref<string | null>(null)

const isOwner = computed(() => {
  const ownerId = setup.value?.userId || setup.value?.user?.id
  return !!ownerId && ownerId === userStore.userId
})

const creatorUsername = computed(() => {
  if (!setup.value?.user?.email) return ''
  const email = setup.value.user.email
  return email.split('@')[0]
})

const creatorAvatarUrl = computed(() => {
  if (!setup.value?.user?.avatarUrl) return null
  return resolveImageUrl(setup.value.user.avatarUrl)
})

const formattedCreatedDate = computed(() => {
  if (!setup.value?.createdAt) return ''
  const date = new Date(setup.value.createdAt)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const year = date.getFullYear()
  return `${month}/${day}/${year}`
})

function resolveImageUrl(url: string | null | undefined, bustCache: boolean = false): string {
  if (!url) return ''
  // Already a full URL
  if (url.startsWith('http://') || url.startsWith('https://')) {
    console.log('[resolveImageUrl] Already full URL:', url)
    return url
  }
  // Relative URL - prepend base URL
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com'
  const cleanUrl = url.replace(/^\/+/, '') // Remove leading slashes
  const fullUrl = `${baseUrl}/${cleanUrl}`
  
  console.log('[resolveImageUrl] Converting:', url, '->', fullUrl)
  
  // Optionally add cache-busting parameter (e.g., after upload)
  if (bustCache) {
    const finalUrl = `${fullUrl}?t=${Date.now()}`
    console.log('[resolveImageUrl] With cache-bust:', finalUrl)
    return finalUrl
  }
  return fullUrl
}

const handleImageError = (e: Event) => {
  const img = e.target as HTMLImageElement
  console.warn('[Image Error] Failed to load image:', img.src)
  // Hide the parent container
  if (img.parentElement) {
    img.parentElement.style.display = 'none'
  }
}

function onNewItemFileChange(e: Event) {
  const target = e.target as HTMLInputElement
  if (target.files && target.files[0]) {
    const file = target.files[0]
    const maxSize = 10 * 1024 * 1024 // 10MB in bytes
    
    if (file.size > maxSize) {
      alertStore.show('Image file size must be less than 10MB.', 'error')
      target.value = '' // Clear the file input
      newItemImageFile.value = null
      newItemImagePreview.value = null
      return
    }
    
    newItemImageFile.value = file
    newItemImagePreview.value = URL.createObjectURL(file)
  } else {
    newItemImageFile.value = null
    newItemImagePreview.value = null
  }
}

const loadSetup = async (bustCache: boolean = false) => {
  const res = await axios.get(`/api/setups/${route.params.id}`, {
    headers: { Authorization: `Bearer ${userStore.token}` },
  })
  setup.value = res.data
  
  // Process items and fix their image URLs
  const items = res.data.items || (Array.isArray(res.data.setupItems)
    ? res.data.setupItems.map((si: any) => si.item)
    : [])
  
  setupItems.value = items.map((item: any) => ({
    ...item,
    imageUrl: item.imageUrl ? resolveImageUrl(item.imageUrl, bustCache) : null
  }))
  
  editTitle.value = setup.value?.title || setup.value?.name || ''
  editDescription.value = setup.value?.description || ''
  editCategory.value = setup.value?.category || ''
}

// item modifications are handled in ItemDetailView

function openDeleteDialog() {
  confirmMessage.value = 'Are you sure you want to delete this setup?'
  showConfirmDialog.value = true
}

async function confirmDelete() {
  try {
    await axios.delete(`/api/setups/${route.params.id}`, {
      headers: { Authorization: `Bearer ${userStore.token}` },
    })
    alertStore.show('Setup deleted.', 'success')
    
    // Determine where to navigate back to based on origin
    const from = route.query.from as string | undefined
    
    if (from === 'feed') {
      router.replace({ name: 'feed', query: { category: route.query.category, maxAgeDays: route.query.maxAgeDays, minAgeDays: route.query.minAgeDays } })
    } else if (from === 'upvoted') {
      router.replace({ name: 'upvoted' })
    } else if (from === 'home') {
      router.replace({ name: 'Home' })
    } else {
      router.replace({ name: 'Setups' })
    }
  } catch (err: any) {
    alertStore.show('Failed to delete setup.', 'error')
    console.error('Error deleting setup:', err)
  } finally {
    showConfirmDialog.value = false
  }
}

const cancelSetupEdit = () => {
  editTitle.value = setup.value?.title || setup.value?.name || ''
  editDescription.value = setup.value?.description || ''
  editCategory.value = setup.value?.category || ''
  editMode.value = false
}

const saveSetupEdit = async () => {
  try {
    // Preserve current items and tags to avoid clearing them in PUT
    const itemIds = (setup.value?.items?.map((i: any) => i.id))
      || (Array.isArray(setupItems.value) ? setupItems.value.map((i: any) => i.id) : [])
    const tagNames = (setup.value?.tags?.map((t: any) => t.name)) || []

    await axios.put(
      `/api/setups/${route.params.id}`,
      {
        title: editTitle.value,
        description: editDescription.value,
        category: editCategory.value,
        itemIds,
        tagNames,
      },
      { headers: { Authorization: `Bearer ${userStore.token}` } },
    )
    await loadSetup()
    editMode.value = false
  } catch (err) {
    alertStore.show?.('Failed to update setup', 'error')
    console.error(err)
  }
}

// Image upload/delete for setup
const setupImageFile = ref<File | null>(null)
const setupImageUploading = ref(false)
const onSetupFileChange = (e: Event) => {
  const target = e.target as HTMLInputElement
  if (target.files && target.files[0]) {
    const file = target.files[0]
    const maxSize = 10 * 1024 * 1024 // 10MB in bytes
    
    if (file.size > maxSize) {
      alertStore.show('Image file size must be less than 10MB.', 'error')
      target.value = '' // Clear the file input
      setupImageFile.value = null
      return
    }
    
    setupImageFile.value = file
  } else {
    setupImageFile.value = null
  }
}

const uploadSetupImage = async () => {
  if (!setup.value || !setupImageFile.value) return
  setupImageUploading.value = true
  try {
    const formData = new FormData()
    formData.append('Image', setupImageFile.value)
    await axios.put(`/api/setups/${route.params.id}/image`, formData, {
      headers: {
        Authorization: `Bearer ${userStore.token}`,
        'Content-Type': 'multipart/form-data',
      },
    })
    setupImageFile.value = null
    // Bust cache to ensure new image is loaded
    await loadSetup(true)
  } catch (err: any) {
    const errorMessage = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.title || err.response?.data?.message || 'Failed to upload setup image'
    alertStore.show(errorMessage, 'error')
    console.error(err)
  } finally {
    setupImageUploading.value = false
  }
}

const toggleUpvote = async () => {
  if (!setup.value || !userStore.token) return
  try {
    const res = await axios.post(`/api/setups/${route.params.id}/upvote`, {}, {
      headers: { Authorization: `Bearer ${userStore.token}` },
    })
    setup.value.hasVoted = res.data.voted
    setup.value.voteCount = res.data.voteCount
  } catch (e) {
    console.error(e)
  }
}

const addItem = async () => {
  let itemId: number | null = null
  try {
    let itemRes
    if (newItemImageFile.value) {
      const formData = new FormData()
      formData.append('Name', newItemName.value)
      formData.append('Description', newItemDescription.value)
      formData.append('Image', newItemImageFile.value)
      itemRes = await axios.post('/api/items/upload', formData, {
        headers: {
          Authorization: `Bearer ${userStore.token}`,
          'Content-Type': 'multipart/form-data',
        },
      })
    } else {
      itemRes = await axios.post(
        '/api/items',
        {
          name: newItemName.value,
          description: newItemDescription.value,
        },
        { headers: { Authorization: `Bearer ${userStore.token}` } },
      )
    }
    itemId = itemRes.data.id
    await axios.post(
      `/api/setups/${route.params.id}/items/${itemId}`,
      {},
      { headers: { Authorization: `Bearer ${userStore.token}` } },
    )
    await loadSetup()
    newItemName.value = ''
    newItemDescription.value = ''
    newItemImageFile.value = null
    newItemImagePreview.value = null
  } catch (err: any) {
    const errorMessage = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.title || err.response?.data?.message || 'Failed to add item'
    alertStore.show(errorMessage, 'error')
  }
}

// item modifications are handled in ItemDetailView

onMounted(loadSetup)

const categories = ref<string[]>(["Photography","Gaming","Climbing","Music","Streaming","Podcasting","Woodworking","Cooking","Cycling","Running","Art","Work/Office","Home/Desk","Other"]) // fallback
const editCategory = ref('')
onMounted(async () => {
  try {
    const res = await axios.get('/api/setups/categories')
    if (Array.isArray(res.data) && res.data.length) categories.value = res.data
  } catch {}
})
</script>

<template>
  <div class="pt-20">
    <ConfirmDialog
      v-model="showConfirmDialog"
      :message="confirmMessage"
      @confirm="confirmDelete"
    />
    <div v-if="setup"
      class="flex flex-col lg:flex-row gap-8 max-w-7xl w-full mx-auto mt-10 min-h-[600px]"
    >
      <div
        class="flex-1 bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-md dark:shadow-gray-900 relative flex flex-col justify-start"
      >
        <!-- Creator Profile Display (top right, only if not owner) -->
        <div v-if="!isOwner && setup?.user" class="absolute top-6 right-6 flex flex-col items-center gap-1">
          <div v-if="creatorAvatarUrl" class="w-10 h-10 rounded-full overflow-hidden border-2 border-gray-300 dark:border-gray-600">
            <img 
              :src="creatorAvatarUrl" 
              alt="Creator avatar" 
              class="w-full h-full object-cover"
              @error="handleImageError"
            />
          </div>
          <div v-else class="w-10 h-10 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center text-gray-600 dark:text-gray-300 font-bold text-sm border-2 border-gray-300 dark:border-gray-600">
            {{ creatorUsername.charAt(0).toUpperCase() }}
          </div>
          <span class="text-xs text-gray-600 dark:text-gray-400 font-medium">{{ creatorUsername }}</span>
        </div>

        <!-- Delete button (only for owner in edit mode) -->
        <div class="absolute top-6 right-6 flex gap-2" v-if="isOwner && editMode">
          <button
            @click="openDeleteDialog"
            class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg z-10"
          >
            Delete Setup
          </button>
        </div>
        <router-link
          v-if="$route.query.from === 'feed'"
          :to="{ path: '/feed', query: { category: $route.query.category, maxAgeDays: $route.query.maxAgeDays, minAgeDays: $route.query.minAgeDays } }"
          class="text-blue-600 dark:text-blue-400 hover:underline mb-4 self-start"
          >&larr; Back to Feed</router-link>
        <router-link
          v-else-if="$route.query.from === 'upvoted'"
          :to="{ path: '/upvoted', query: { category: $route.query.category, maxAgeDays: $route.query.maxAgeDays, minAgeDays: $route.query.minAgeDays } }"
          class="text-blue-600 dark:text-blue-400 hover:underline mb-4 self-start"
          >&larr; Back to Upvoted</router-link>
        <router-link
          v-else-if="$route.query.from === 'home'"
          :to="{ path: '/', query: { category: $route.query.category, maxAgeDays: $route.query.maxAgeDays, minAgeDays: $route.query.minAgeDays } }"
          class="text-blue-600 dark:text-blue-400 hover:underline mb-4 self-start"
          >&larr; Back to Home</router-link>
        <router-link
          v-else
          to="/setups"
          class="text-blue-600 dark:text-blue-400 hover:underline mb-4 self-start"
          >&larr; Back to Setups</router-link>
        <div v-if="setup?.imageUrl" class="flex justify-center mb-4">
          <img
            :src="resolveImageUrl(setup.imageUrl)"
            alt="Setup image"
            class="max-h-60 object-contain rounded-lg"
            @error="handleImageError"
            loading="lazy"
          />
        </div>
  <div v-if="isOwner && editMode" class="mb-6">
          <h3 class="font-semibold mb-2 text-gray-900 dark:text-gray-100">Replace setup image (required)</h3>
          <form @submit.prevent="uploadSetupImage" class="flex flex-col sm:flex-row gap-2">
            <input type="file" accept="image/*" @change="onSetupFileChange" required class="bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded p-2" />
            <button
              type="submit"
              class="bg-blue-600 dark:bg-blue-500 text-white px-3 py-2 rounded disabled:opacity-50 hover:bg-blue-700 dark:hover:bg-blue-600"
              :disabled="!setupImageFile || setupImageUploading"
            >
              {{ setupImageUploading ? 'Uploading...' : 'Upload New Image' }}
            </button>
          </form>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Images are required for all setups</p>
        </div>
        <div class="flex items-start justify-between gap-4">
          <h1 class="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">
            {{ setup?.title || setup?.name }}
          </h1>
          <div class="flex items-center gap-2">
            <button @click="toggleUpvote" class="px-3 py-2 rounded border border-gray-300 dark:border-gray-600" :class="{ 'bg-blue-600 dark:bg-blue-500 text-white': setup?.hasVoted, 'text-gray-700 dark:text-gray-300': !setup?.hasVoted }">
              ▲ {{ setup?.voteCount || 0 }}
            </button>
          <button
            v-if="isOwner && !editMode"
            @click="editMode = true"
            class="h-10 px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded hover:bg-blue-700 dark:hover:bg-blue-600"
          >
            Edit
          </button>
          </div>
        </div>
        <p class="text-gray-600 dark:text-gray-400 mb-6">{{ setup?.description }}</p>
        <div v-if="isOwner && editMode" class="border-t border-gray-200 dark:border-gray-700 pt-6 mt-2">
          <h2 class="text-xl font-semibold mb-3 text-gray-900 dark:text-gray-100">Edit setup</h2>
          <form @submit.prevent="saveSetupEdit" class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input v-model="editTitle" class="border border-gray-300 dark:border-gray-600 rounded p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" placeholder="Title" />
            <input v-model="editDescription" class="border border-gray-300 dark:border-gray-600 rounded p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" placeholder="Description" />
            <select v-model="editCategory" required class="border border-gray-300 dark:border-gray-600 rounded p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100">
              <option value="" disabled>Select category</option>
              <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
            </select>
            <div class="md:col-span-2 flex gap-2">
              <button type="submit" class="bg-green-600 dark:bg-green-500 text-white px-4 py-2 rounded hover:bg-green-700 dark:hover:bg-green-600">Save</button>
              <button type="button" @click="cancelSetupEdit" class="bg-gray-200 dark:bg-gray-600 text-gray-800 dark:text-gray-100 px-4 py-2 rounded hover:bg-gray-300 dark:hover:bg-gray-700">Cancel</button>
            </div>
          </form>
        </div>
  <div v-if="setupItems.length && !editMode" class="mb-6">
          <h2 class="text-lg font-semibold mb-2 text-gray-900 dark:text-gray-100">Items in this setup:</h2>
          <ul class="list-disc list-inside text-gray-700 dark:text-gray-300">
            <li v-for="item in setupItems" :key="item.id" class="mb-2">
              <router-link
                :to="{ path: `/items/${item.id}`, query: $route.query }"
                class="text-blue-700 dark:text-blue-400 hover:underline font-medium"
              >
                {{ item.name }}
              </router-link>
            </li>
          </ul>
        </div>
        
        <!-- Created date in bottom right -->
        <div v-if="formattedCreatedDate" class="mt-auto pt-4 text-right">
          <span class="text-xs text-gray-400 dark:text-gray-500">{{ formattedCreatedDate }}</span>
        </div>
      </div>
      <div
        v-if="!editMode && isOwner"
        class="w-full lg:w-[400px] bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-md dark:shadow-gray-900 flex flex-col items-stretch h-fit self-start"
      >
        <h2 class="text-xl font-bold mb-4 text-gray-800 dark:text-gray-100">Add Item</h2>
        <form @submit.prevent="addItem" class="flex flex-col gap-4">
          <input
            v-model="newItemName"
            type="text"
            placeholder="Item name"
            class="border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
            required
          />
          <input
            v-model="newItemDescription"
            type="text"
            placeholder="Description (optional)"
            class="border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
          />
          <input
            type="file"
            accept="image/*"
            @change="onNewItemFileChange"
            class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
          />
          <div
            v-if="newItemImagePreview"
            class="mt-2 flex justify-center"
          >
            <img
              :src="newItemImagePreview"
              alt="Preview"
              class="max-h-32 rounded-lg object-contain"
            />
          </div>
          <button
            type="submit"
            class="bg-blue-600 dark:bg-blue-500 text-white rounded-lg p-2 font-semibold hover:bg-blue-700 dark:hover:bg-blue-600"
          >
            Add Item
          </button>
        </form>
      </div>
    </div>

    <!-- Comment Section -->
    <div v-if="setup" class="mt-6 mb-8">
      <CommentSection :setup-id="setup.id" />
    </div>

    <div v-else class="flex justify-center items-center min-h-[400px]">
      <div class="text-gray-500 dark:text-gray-400 text-lg">Loading...</div>
    </div>
  </div>
</template>

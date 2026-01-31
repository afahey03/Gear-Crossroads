<template>
  <div class="pt-20">
    <ConfirmDialog
      v-model="showConfirmDialog"
      :message="confirmMessage"
      @confirm="confirmDelete"
    />
    <div class="p-8 max-w-6xl mx-auto">
      <div class="flex justify-between items-center mb-8">
        <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">My Setups</h1>
        <button
          @click="showForm = !showForm"
          class="bg-blue-600 dark:bg-blue-500 hover:bg-blue-700 dark:hover:bg-blue-600 text-white font-semibold px-4 py-2 rounded-lg shadow"
        >
          {{ showForm ? 'Cancel' : '+ New Setup' }}
        </button>
      </div>

      <transition name="fade">
        <div
          v-if="showForm"
          class="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-md dark:shadow-gray-900 mb-8 border border-gray-200 dark:border-gray-700"
        >
          <form @submit.prevent="createSetup" class="space-y-4">
            <div>
              <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Title <span class="text-red-500">*</span></label>
              <input
                v-model="newSetup.Title"
                required
                class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                placeholder="Setup title"
              />
            </div>

            <div>
              <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Description</label>
              <textarea
                v-model="newSetup.Description"
                class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                placeholder="Describe your setup..."
              ></textarea>
            </div>

            <div>
              <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Category <span class="text-red-500">*</span></label>
              <select v-model="newSetup.Category" required class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100">
                <option value="" disabled>Select a category</option>
                <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
              </select>
            </div>


            <div>
              <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Image <span class="text-red-500">*</span></label>
              <input
                type="file"
                accept="image/*"
                @change="onFileChange"
                required
                class="w-full border border-gray-300 dark:border-gray-600 rounded-lg p-2 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
              />
              <div v-if="imagePreview" class="mt-2 flex justify-center">
                <img :src="imagePreview" alt="Preview" class="max-h-40 rounded-lg object-contain" />
              </div>
            </div>

            <button
              type="submit"
              class="bg-green-600 dark:bg-green-500 hover:bg-green-700 dark:hover:bg-green-600 text-white px-4 py-2 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="!imageFile"
            >
              Create
            </button>
          </form>
        </div>
      </transition>

      <div v-if="loading" class="text-gray-500 dark:text-gray-400 text-center mt-8">Loading...</div>
      <div v-else-if="setups.length" class="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div v-for="setup in setups" :key="setup.id">
          <SetupCard
            :title="setup.title"
            :description="setup.description"
            :imageUrl="setup.imageUrl ? resolveImageUrl(setup.imageUrl) : ''"
            :setupId="setup.id"
          />
        </div>
      </div>
      <p v-else class="text-gray-500 dark:text-gray-400 text-center mt-8">
        No setups yet. Create your first one above!
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">

import { ref, onMounted } from 'vue'
import axios from 'axios'
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'


const userStore = useUserStore()
const alertStore = useAlertStore()
const setups = ref<any[]>([])
const loading = ref(true)
const showForm = ref(false)


const newSetup = ref({
  Title: '',
  Description: '',
  Category: ''
})
const categories = ref<string[]>(["Photography","Gaming","Climbing","Music","Streaming","Magic: The Gathering","Disc Golf","Fishing/Tackle","Podcasting","Woodworking","Cooking","Cycling","Running","Art","Work/Office","Home/Desk","Other"]) // fallback
const imageFile = ref<File|null>(null)
const imagePreview = ref<string|null>(null)

function onFileChange(e: Event) {
  const target = e.target as HTMLInputElement
  if (target.files && target.files[0]) {
    const file = target.files[0]
    const maxSize = 10 * 1024 * 1024 // 10MB in bytes
    
    if (file.size > maxSize) {
      alertStore.show('Image file size must be less than 10MB.', 'error')
      target.value = '' // Clear the file input
      imageFile.value = null
      imagePreview.value = null
      return
    }
    
    imageFile.value = file
    imagePreview.value = URL.createObjectURL(file)
  } else {
    imageFile.value = null
    imagePreview.value = null
  }
}

const fetchSetups = async () => {
  loading.value = true
  try {
    const headers: any = {}
    if (userStore.token) headers.Authorization = `Bearer ${userStore.token}`
    const res = await axios.get('/api/setups/mine', { headers })
    setups.value = res.data
  } catch (err) {
    console.error('Error fetching setups:', err)
  } finally {
    loading.value = false
  }
}

const createSetup = async () => {
  try {
    if (!userStore.token) {
      alertStore.show('You must be logged in to create a setup.', 'error')
      return
    }

    const formData = new FormData()
    formData.append('Title', newSetup.value.Title)
    formData.append('Description', newSetup.value.Description)
    formData.append('Category', newSetup.value.Category)
    if (imageFile.value) {
      formData.append('Image', imageFile.value)
    }

    await axios.post('/api/setups', formData, {
      headers: {
        Authorization: `Bearer ${userStore.token}`,
        'Content-Type': 'multipart/form-data'
      }
    })

    newSetup.value = { Title: '', Description: '', Category: '' }
    imageFile.value = null
    imagePreview.value = null
    showForm.value = false

    await fetchSetups()
  } catch (err: any) {
    console.error('Error creating setup:', err)
    const errorMessage = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.title || err.response?.data?.message || 'Failed to create setup — check console for details.'
    alertStore.show(errorMessage, 'error')
  }
}

const showConfirmDialog = ref(false)
const confirmMessage = ref('')
let setupIdToDelete: number | null = null

/*function openDeleteDialog(id: number) {
  if (!userStore.token) {
    alertStore.show('You must be logged in to delete a setup.', 'error')
    return
  }
  setupIdToDelete = id
  confirmMessage.value = 'Are you sure you want to delete this setup?'
  showConfirmDialog.value = true
}*/

async function confirmDelete() {
  if (setupIdToDelete == null) return
  try {
    await axios.delete(`/api/setups/${setupIdToDelete}`, {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    setups.value = setups.value.filter(s => s.id !== setupIdToDelete)
    alertStore.show('Setup deleted.', 'success')
  } catch (err: any) {
    alertStore.show('Failed to delete setup.', 'error')
    console.error('Error deleting setup:', err)
  } finally {
    setupIdToDelete = null
    showConfirmDialog.value = false
  }
}

onMounted(fetchSetups)
import ConfirmDialog from '../components/ConfirmDialog.vue'
import SetupCard from '../components/SetupCard.vue'
function resolveImageUrl(url: string | null | undefined): string {
  if (!url) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com'
  return `${baseUrl}/${url.replace(/^\/+/, '')}`
}

// Load categories from API with graceful fallback
onMounted(async () => {
  try {
    const res = await axios.get('/api/setups/categories')
    if (Array.isArray(res.data) && res.data.length) categories.value = res.data
  } catch {}
})
</script>

<style>
.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>

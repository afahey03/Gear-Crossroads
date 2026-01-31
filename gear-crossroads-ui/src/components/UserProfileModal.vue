<template>
  <!-- Delete Confirmation Modal -->
  <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black bg-opacity-50 dark:bg-opacity-70 flex items-center justify-center z-[60] p-4">
    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-md border-2 border-red-500 dark:border-red-600">
      <h3 class="text-xl font-bold mb-4 text-red-600 dark:text-red-400">⚠️ Delete Account</h3>
      <p class="text-gray-700 dark:text-gray-300 mb-4">Are you sure you want to delete your account? This action cannot be undone.</p>
      <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded p-3 mb-4">
        <p class="text-sm text-red-800 dark:text-red-300 font-semibold mb-2">This will:</p>
        <ul class="text-sm text-red-700 dark:text-red-400 space-y-1 list-disc list-inside">
          <li>Delete all your setups and items</li>
          <li>Remove all your upvotes</li>
          <li>Replace your username with "Deleted User" on your comments</li>
        </ul>
      </div>
      <div class="flex gap-3">
        <button type="button" @click="showDeleteConfirm = false" class="flex-1 px-4 py-2 rounded bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-700 text-gray-800 dark:text-gray-100 font-medium">
          Cancel
        </button>
        <button type="button" @click="handleDelete" class="flex-1 px-4 py-2 rounded bg-red-600 dark:bg-red-700 hover:bg-red-700 dark:hover:bg-red-800 text-white font-medium">
          Delete Account
        </button>
      </div>
    </div>
  </div>

  <!-- Main Profile Modal -->
  <div class="fixed inset-0 bg-black bg-opacity-40 dark:bg-opacity-60 flex items-center justify-center z-50 p-4">
    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 md:p-8 w-full max-w-md relative max-h-[90vh] overflow-y-auto">
      <button class="absolute top-2 right-2 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 z-10" @click="$emit('close')">
        <span class="text-2xl">&times;</span>
      </button>
      <h2 class="text-xl md:text-2xl font-bold mb-4 md:mb-6 text-center pr-8 text-gray-900 dark:text-gray-100">Edit Profile</h2>
      <form @submit.prevent="saveProfile" class="space-y-4">
        <div>
          <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Display Name</label>
          <input v-model="displayName" type="text" class="w-full border border-gray-300 dark:border-gray-600 rounded px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400" />
        </div>
        <div>
          <label class="block text-gray-700 dark:text-gray-300 font-medium mb-1">Profile Picture</label>
          <input type="file" accept="image/*" @change="onFileChange" class="w-full border border-gray-300 dark:border-gray-600 rounded px-3 py-2 text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100" />
          <div v-if="avatarPreview" class="mt-2 flex justify-center">
            <img :src="avatarPreview" alt="Avatar Preview" class="w-20 h-20 rounded-full object-cover border border-gray-300 dark:border-gray-600" />
          </div>
        </div>
        <div class="flex flex-col sm:flex-row justify-between gap-2">
          <button type="button" @click="confirmDelete" class="px-4 py-2 rounded bg-red-600 dark:bg-red-700 hover:bg-red-700 dark:hover:bg-red-800 text-white order-3 sm:order-1">Delete Account</button>
          <div class="flex flex-col sm:flex-row gap-2 sm:space-x-2 order-1 sm:order-2">
            <button type="button" @click="$emit('close')" class="px-4 py-2 rounded bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-700 text-gray-800 dark:text-gray-100 order-2 sm:order-1">Cancel</button>
            <button type="submit" class="px-4 py-2 rounded bg-blue-600 dark:bg-blue-500 text-white hover:bg-blue-700 dark:hover:bg-blue-600 order-1 sm:order-2">Save</button>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, defineProps } from 'vue'
import { useAlertStore } from '../stores/alert'
import { useUserStore } from '../stores/user'
import { useRouter } from 'vue-router'

const alertStore = useAlertStore()
const userStore = useUserStore()
const router = useRouter()

const props = defineProps({
  displayName: String,
  avatarUrl: String
})

const emit = defineEmits(['save', 'close'])

const displayName = ref(props.displayName || '')
const avatarFile = ref<File|null>(null)
const avatarPreview = ref(props.avatarUrl || '')
const showDeleteConfirm = ref(false)

watch(() => props.displayName, val => displayName.value = val ?? '')
watch(() => props.avatarUrl, val => avatarPreview.value = val ?? '')

function onFileChange(e: Event) {
  const target = e.target as HTMLInputElement
  const files = target.files
  if (files && files[0]) {
    const file = files[0]
    const maxSize = 10 * 1024 * 1024 // 10MB in bytes
    
    if (file.size > maxSize) {
      alertStore.show('Image file size must be less than 10MB.', 'error')
      target.value = '' // Clear the file input
      avatarFile.value = null
      avatarPreview.value = props.avatarUrl || ''
      return
    }
    
    avatarFile.value = file
    avatarPreview.value = URL.createObjectURL(file)
  }
}

function saveProfile() {
  emit('save', { displayName: displayName.value, avatar: avatarFile.value })
}

function confirmDelete() {
  showDeleteConfirm.value = true
}

async function handleDelete() {
  showDeleteConfirm.value = false
  try {
    await userStore.deleteAccount()
    alertStore.show('Your account has been deleted successfully.', 'success')
    emit('close')
    router.push('/')
  } catch (error: any) {
    alertStore.show(error.response?.data?.message || 'Failed to delete account. Please try again.', 'error')
  }
}
</script>

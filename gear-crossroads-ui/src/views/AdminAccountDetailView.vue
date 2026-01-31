<template>
  <div class="pt-20">
    <div class="max-w-7xl mx-auto p-6">
      <router-link to="/admin/accounts" class="text-blue-600 dark:text-blue-400 hover:underline mb-4 inline-block">
        ← Back to Accounts
      </router-link>
      
      <div v-if="loading" class="text-gray-500 dark:text-gray-400">Loading...</div>
      
      <div v-else-if="account">
        <!-- User Info Card -->
        <div class="bg-white dark:bg-gray-800 rounded-xl shadow dark:shadow-gray-900 p-4 md:p-6 mb-6">
          <!-- Desktop Layout -->
          <div class="hidden md:flex items-start justify-between">
            <div class="flex items-center gap-4">
              <img 
                v-if="account.user.avatarUrl"
                :src="resolveImageUrl(account.user.avatarUrl)" 
                alt="Avatar"
                class="w-20 h-20 rounded-full object-cover"
              />
              <div class="w-20 h-20 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center text-2xl font-bold text-white" v-else>
                {{ (account.user.displayName || extractUsername(account.user.email) || '?')[0].toUpperCase() }}
              </div>
              <div>
                <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ extractUsername(account.user.email) }}</h1>
                <p v-if="account.user.displayName" class="text-gray-600 dark:text-gray-400">{{ account.user.displayName }}</p>
                <p class="text-gray-500 dark:text-gray-400 text-sm">{{ account.user.email }}</p>
                <div class="mt-2 flex gap-2 flex-wrap">
                  <span v-if="account.user.isAdmin" class="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300 rounded">
                    ADMIN
                  </span>
                  <span v-if="account.user.isWife" class="ml-2 px-2 py-1 text-xs bg-pink-100 dark:bg-pink-900/50 text-pink-800 dark:text-pink-300 rounded">
                    WIFE
                  </span>
                  <span v-if="account.user.isBanned" class="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300 rounded">
                    BANNED
                  </span>
                  <span v-else class="px-2 py-1 text-xs bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300 rounded">
                    Active
                  </span>
                  <span v-if="!account.user.emailConfirmed" class="px-2 py-1 text-xs bg-yellow-100 dark:bg-yellow-900/50 text-yellow-800 dark:text-yellow-300 rounded">
                    Email Not Confirmed
                  </span>
                </div>
                <p v-if="account.user.bio" class="mt-2 text-gray-700 dark:text-gray-300">{{ account.user.bio }}</p>
                <p v-if="account.user.bannedAt" class="mt-2 text-red-600 dark:text-red-400 text-sm">
                  Banned on: {{ new Date(account.user.bannedAt).toLocaleString() }}
                </p>
              </div>
            </div>
            
            <div class="flex gap-2">
              <button
                v-if="!account.user.isBanned && !account.user.isAdmin"
                @click="showBanConfirm = true"
                class="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700"
              >
                Ban User
              </button>
              <button
                v-if="account.user.isBanned"
                @click="unbanUser"
                class="bg-green-600 dark:bg-green-500 text-white px-4 py-2 rounded hover:bg-green-700 dark:hover:bg-green-600"
              >
                Unban User
              </button>
            </div>
          </div>

          <!-- Mobile Layout -->
          <div class="md:hidden">
            <div class="flex items-start gap-3 mb-4">
              <img 
                v-if="account.user.avatarUrl"
                :src="resolveImageUrl(account.user.avatarUrl)" 
                alt="Avatar"
                class="w-16 h-16 rounded-full object-cover flex-shrink-0"
              />
              <div class="w-16 h-16 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center text-xl font-bold text-white flex-shrink-0" v-else>
                {{ (account.user.displayName || extractUsername(account.user.email) || '?')[0].toUpperCase() }}
              </div>
              <div class="flex-1 min-w-0">
                <h1 class="text-xl font-bold truncate text-gray-900 dark:text-gray-100">{{ extractUsername(account.user.email) }}</h1>
                <p v-if="account.user.displayName" class="text-gray-600 dark:text-gray-400 text-sm truncate">{{ account.user.displayName }}</p>
                <p class="text-gray-500 dark:text-gray-400 text-xs break-all">{{ account.user.email }}</p>
              </div>
            </div>
            
            <div class="flex gap-2 flex-wrap mb-3">
              <span v-if="account.user.isAdmin" class="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300 rounded">
                ADMIN
              </span>
              <span v-if="account.user.isWife" class="px-2 py-1 text-xs bg-pink-100 dark:bg-pink-900/50 text-pink-800 dark:text-pink-300 rounded">
                WIFE
              </span>
              <span v-if="account.user.isBanned" class="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300 rounded">
                BANNED
              </span>
              <span v-else class="px-2 py-1 text-xs bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300 rounded">
                Active
              </span>
              <span v-if="!account.user.emailConfirmed" class="px-2 py-1 text-xs bg-yellow-100 dark:bg-yellow-900/50 text-yellow-800 dark:text-yellow-300 rounded">
                Email Not Confirmed
              </span>
            </div>
            
            <p v-if="account.user.bio" class="text-gray-700 dark:text-gray-300 text-sm mb-3">{{ account.user.bio }}</p>
            <p v-if="account.user.bannedAt" class="text-red-600 dark:text-red-400 text-sm mb-3">
              Banned on: {{ new Date(account.user.bannedAt).toLocaleString() }}
            </p>
            
            <div class="flex gap-2">
              <button
                v-if="!account.user.isBanned && !account.user.isAdmin"
                @click="showBanConfirm = true"
                class="flex-1 bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700 text-sm"
              >
                Ban User
              </button>
              <button
                v-if="account.user.isBanned"
                @click="unbanUser"
                class="flex-1 bg-green-600 dark:bg-green-500 text-white px-4 py-2 rounded hover:bg-green-700 dark:hover:bg-green-600 text-sm"
              >
                Unban User
              </button>
            </div>
          </div>
        </div>
        
        <!-- Ban Confirmation Dialog -->
        <div v-if="showBanConfirm" class="fixed inset-0 bg-black bg-opacity-50 dark:bg-opacity-70 flex items-center justify-center z-50">
          <div class="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-md w-full mx-4">
            <h2 class="text-xl font-bold mb-4 text-red-600 dark:text-red-400">⚠️ Confirm Ban</h2>
            <p class="mb-4 text-gray-900 dark:text-gray-100">
              Are you sure you want to ban <strong>{{ extractUsername(account.user.email) }}</strong>?
            </p>
            <p class="mb-4 text-sm text-gray-600 dark:text-gray-400">
              This will:
            </p>
            <ul class="mb-6 text-sm text-gray-600 dark:text-gray-400 list-disc list-inside">
              <li>Prevent the user from logging in</li>
              <li>Delete all {{ account.setups.length }} of their setups</li>
              <li>This action cannot be undone</li>
            </ul>
            <div class="flex gap-2 justify-end">
              <button
                @click="showBanConfirm = false"
                class="px-4 py-2 bg-gray-200 dark:bg-gray-600 text-gray-800 dark:text-gray-100 rounded hover:bg-gray-300 dark:hover:bg-gray-700"
              >
                Cancel
              </button>
              <button
                @click="banUser"
                class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
                :disabled="banning"
              >
                {{ banning ? 'Banning...' : 'Ban User' }}
              </button>
            </div>
          </div>
        </div>
        
        <!-- Setups List -->
        <div class="mb-6">
          <h2 class="text-xl md:text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">Setups ({{ account.setups.length }})</h2>
          
          <div v-if="account.setups.length" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 md:gap-6">
            <div v-for="setup in account.setups" :key="setup.id" class="bg-white dark:bg-gray-800 rounded-xl shadow dark:shadow-gray-900 p-4">
              <router-link :to="`/setups/${setup.id}`" class="block">
                <div v-if="setup.imageUrl" class="mb-2 flex justify-center">
                  <img :src="resolveImageUrl(setup.imageUrl)" class="h-40 object-contain rounded-lg" />
                </div>
                <h3 class="font-semibold text-base md:text-lg mb-2 text-gray-900 dark:text-gray-100">{{ setup.title }}</h3>
                <p class="text-gray-600 dark:text-gray-400 text-sm mb-2 line-clamp-2">{{ setup.description }}</p>
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400 text-xs md:text-sm">{{ setup.category }}</span>
                  <span class="text-gray-500 dark:text-gray-400 text-xs md:text-sm">▲ {{ setup.voteCount }}</span>
                </div>
                <div class="text-xs text-gray-400 dark:text-gray-500 mt-2">
                  {{ new Date(setup.createdAt).toLocaleDateString() }}
                </div>
              </router-link>
            </div>
          </div>
          
          <div v-else class="text-gray-500 dark:text-gray-400">This user has no setups.</div>
        </div>
      </div>
      
      <div v-else class="text-gray-500 dark:text-gray-400">Account not found.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const alertStore = useAlertStore()

const account = ref<any>(null)
const loading = ref(true)
const showBanConfirm = ref(false)
const banning = ref(false)

const extractUsername = (email: string) => {
  if (!email) return ''
  return email.split('@')[0]
}

const resolveImageUrl = (url: string | null | undefined): string => {
  if (!url) return ''
  if (url.startsWith('http://') || url.startsWith('https://')) return url
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'https://gearcrossroads-api.onrender.com'
  return `${baseUrl}/${url.replace(/^\/+/, '')}`
}

const loadAccount = async () => {
  try {
    const res = await axios.get(`/api/admin/accounts/${route.params.id}`, {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    account.value = res.data
  } catch (err) {
    console.error('Failed to load account:', err)
  } finally {
    loading.value = false
  }
}

const banUser = async () => {
  if (!account.value) return
  
  banning.value = true
  try {
    await axios.post(`/api/admin/accounts/${route.params.id}/ban`, {}, {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    
    alertStore.show('User has been banned and their setups deleted.', 'success')
    showBanConfirm.value = false
    await loadAccount()
  } catch (err: any) {
    alertStore.show(err.response?.data?.message || 'Failed to ban user', 'error')
  } finally {
    banning.value = false
  }
}

const unbanUser = async () => {
  if (!account.value) return
  
  try {
    await axios.post(`/api/admin/accounts/${route.params.id}/unban`, {}, {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    
    alertStore.show('User has been unbanned.', 'success')
    await loadAccount()
  } catch (err: any) {
    alertStore.show(err.response?.data?.message || 'Failed to unban user', 'error')
  }
}

onMounted(async () => {
  if (!userStore.isAdmin) {
    router.push('/')
    return
  }
  
  await loadAccount()
})
</script>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>

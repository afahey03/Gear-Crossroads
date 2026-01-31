<template>
  <div class="pt-20">
    <div class="max-w-7xl mx-auto p-6">
      <h1 class="text-3xl font-bold mb-6 text-gray-900 dark:text-gray-100">Account Management</h1>
      
      <div v-if="loading" class="text-gray-500 dark:text-gray-400">Loading accounts...</div>
      
      <!-- Desktop Table View -->
      <div v-else-if="accounts.length" class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow dark:shadow-gray-900 overflow-hidden">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-700">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                Username
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                Email
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                Setups
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                Status
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="account in accounts" :key="account.id" :class="{ 'bg-red-50 dark:bg-red-900/20': account.isBanned }">
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="flex items-center">
                  <div>
                    <div class="text-sm font-medium text-gray-900 dark:text-gray-100">
                      {{ extractUsername(account.email) }}
                      <span v-if="account.isAdmin" class="ml-2 px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300 rounded">
                        ADMIN
                      </span>
                      <span v-if="account.isWife" class="ml-2 px-2 py-1 text-xs bg-pink-100 dark:bg-pink-900/50 text-pink-800 dark:text-pink-300 rounded">
                        WIFE
                      </span>
                    </div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="text-sm text-gray-900 dark:text-gray-100">{{ account.email }}</div>
                <div v-if="!account.emailConfirmed" class="text-xs text-red-600 dark:text-red-400">Not confirmed</div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                {{ account.setupCount }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span v-if="account.isBanned" class="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300 rounded">
                  BANNED
                </span>
                <span v-else class="px-2 py-1 text-xs bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300 rounded">
                  Active
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                <router-link
                  :to="`/admin/accounts/${account.id}`"
                  class="text-blue-600 dark:text-blue-400 hover:text-blue-900 dark:hover:text-blue-300"
                >
                  View
                </router-link>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Mobile Card View -->
      <div v-if="!loading && accounts.length" class="md:hidden space-y-4">
        <div 
          v-for="account in accounts" 
          :key="account.id"
          class="bg-white dark:bg-gray-800 rounded-lg shadow dark:shadow-gray-900 p-4"
          :class="{ 'bg-red-50 dark:bg-red-900/20': account.isBanned }"
        >
          <div class="flex justify-between items-start mb-3">
            <div class="flex-1">
              <div class="font-semibold text-lg text-gray-900 dark:text-gray-100 flex items-center gap-2">
                {{ extractUsername(account.email) }}
                <span v-if="account.isAdmin" class="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300 rounded">
                  ADMIN
                </span>
                <span v-if="account.isWife" class="px-2 py-1 text-xs bg-pink-100 dark:bg-pink-900/50 text-pink-800 dark:text-pink-300 rounded">
                  WIFE
                </span>
              </div>
              <div class="text-sm text-gray-600 dark:text-gray-400 break-all">{{ account.email }}</div>
              <div v-if="!account.emailConfirmed" class="text-xs text-red-600 dark:text-red-400 mt-1">Email not confirmed</div>
            </div>
            <div class="flex flex-col items-end gap-2">
              <span v-if="account.isBanned" class="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300 rounded whitespace-nowrap">
                BANNED
              </span>
              <span v-else class="px-2 py-1 text-xs bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300 rounded whitespace-nowrap">
                Active
              </span>
            </div>
          </div>
          
          <div class="flex justify-between items-center pt-3 border-t border-gray-200 dark:border-gray-700">
            <div class="text-sm text-gray-500 dark:text-gray-400">
              <span class="font-medium">{{ account.setupCount }}</span> setups
            </div>
            <router-link
              :to="`/admin/accounts/${account.id}`"
              class="text-blue-600 dark:text-blue-400 hover:text-blue-900 dark:hover:text-blue-300 font-medium text-sm"
            >
              View Details →
            </router-link>
          </div>
        </div>
      </div>
      
      <div v-else-if="!loading && !accounts.length" class="text-gray-500 dark:text-gray-400">No accounts found.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { useUserStore } from '../stores/user'

const userStore = useUserStore()
const router = useRouter()
const accounts = ref<any[]>([])
const loading = ref(true)

const extractUsername = (email: string) => {
  if (!email) return ''
  return email.split('@')[0]
}

onMounted(async () => {
  if (!userStore.isAdmin) {
    router.push('/')
    return
  }
  
  try {
    const res = await axios.get('/api/admin/accounts', {
      headers: { Authorization: `Bearer ${userStore.token}` }
    })
    accounts.value = res.data
  } catch (err) {
    console.error('Failed to load accounts:', err)
  } finally {
    loading.value = false
  }
})
</script>

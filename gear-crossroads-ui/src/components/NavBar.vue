<template>
  <nav class="bg-white dark:bg-gray-800 shadow-md dark:shadow-gray-900 fixed w-full top-0 left-0 z-50 transition-colors duration-200">
    <div class="max-w-7xl mx-auto px-6 py-3 flex justify-between items-center">
      <router-link to="/" class="flex items-center space-x-2">
        <img src="/assets/gc-transp-logo.png" alt="Gear Crossroads Logo" class="h-8 w-8" />
        <span class="text-2xl font-bold text-blue-600 dark:text-blue-400">Gear Crossroads</span>
      </router-link>

      <!-- Desktop menu -->
      <div class="hidden md:flex items-center space-x-6">
        <router-link to="/" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Home</router-link>
        <router-link to="/feed" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Feed</router-link>
        <router-link to="/upvoted" v-if="userStore.token" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Upvoted</router-link>
        <router-link to="/setups" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">My Setups</router-link>
        <router-link to="/admin/accounts" v-if="userStore.isAdmin" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Accounts</router-link>
        <router-link to="/login" v-if="!userStore.token" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Login</router-link>
        <router-link to="/register" v-if="!userStore.token" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Register</router-link>
        
        <!-- Theme Toggle Button -->
        <button @click="themeStore.toggleTheme()" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none transition-colors" aria-label="Toggle theme">
          <svg v-if="!themeStore.isDark" class="w-6 h-6 text-yellow-500" fill="currentColor" viewBox="0 0 24 24">
            <!-- Light bulb ON (yellow) -->
            <path d="M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7zm2.85 11.1l-.85.6V16h-4v-2.3l-.85-.6C7.8 12.16 7 10.63 7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.63-.8 3.16-2.15 4.1z"/>
          </svg>
          <svg v-else class="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 24 24">
            <!-- Light bulb OFF (gray) -->
            <path d="M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7zm2.85 11.1l-.85.6V16h-4v-2.3l-.85-.6C7.8 12.16 7 10.63 7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.63-.8 3.16-2.15 4.1z"/>
          </svg>
        </button>
        
        <div v-if="userStore.token" class="relative flex items-center space-x-3">
          <button @click="showProfileModal = true" class="flex items-center space-x-2 focus:outline-none">
            <img v-if="userStore.avatarUrl" :src="avatarSrc" alt="avatar" class="w-8 h-8 rounded-full object-cover border border-gray-300 dark:border-gray-600" />
            <div v-else class="w-8 h-8 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center text-gray-600 dark:text-gray-300 font-bold">{{ userInitials }}</div>
            <span class="font-medium text-gray-700 dark:text-gray-300">{{ userStore.displayName || 'Profile' }}</span>
          </button>
          <button @click="logout" class="bg-blue-600 dark:bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 ml-2">Logout</button>
        </div>
      </div>

      <!-- Mobile menu toggle -->
      <button @click="mobileOpen = !mobileOpen" class="md:hidden inline-flex items-center justify-center p-2 rounded hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none" aria-label="Open menu">
        <svg class="h-6 w-6 text-gray-700 dark:text-gray-300" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
    </div>

    <!-- Mobile menu -->
    <div v-if="mobileOpen" class="md:hidden px-6 pb-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
      <div class="flex flex-col space-y-3">
        <router-link @click.native="mobileOpen=false" to="/" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Home</router-link>
        <router-link @click.native="mobileOpen=false" to="/feed" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Feed</router-link>
        <router-link v-if="userStore.token" @click.native="mobileOpen=false" to="/upvoted" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Upvoted</router-link>
        <router-link @click.native="mobileOpen=false" to="/setups" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">My Setups</router-link>
        <router-link v-if="userStore.isAdmin" @click.native="mobileOpen=false" to="/admin/accounts" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Accounts</router-link>
        <router-link v-if="!userStore.token" @click.native="mobileOpen=false" to="/login" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Login</router-link>
        <router-link v-if="!userStore.token" @click.native="mobileOpen=false" to="/register" class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium">Register</router-link>
        
        <!-- Theme Toggle for Mobile -->
        <button @click="themeStore.toggleTheme()" class="flex items-center space-x-2 py-2 focus:outline-none">
          <svg v-if="!themeStore.isDark" class="w-6 h-6 text-yellow-500" fill="currentColor" viewBox="0 0 24 24">
            <path d="M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7zm2.85 11.1l-.85.6V16h-4v-2.3l-.85-.6C7.8 12.16 7 10.63 7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.63-.8 3.16-2.15 4.1z"/>
          </svg>
          <svg v-else class="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 24 24">
            <path d="M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7zm2.85 11.1l-.85.6V16h-4v-2.3l-.85-.6C7.8 12.16 7 10.63 7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.63-.8 3.16-2.15 4.1z"/>
          </svg>
          <span class="font-medium text-gray-700 dark:text-gray-300">{{ themeStore.isDark ? 'Light Mode' : 'Dark Mode' }}</span>
        </button>
        
        <div v-if="userStore.token" class="flex items-center justify-between pt-2">
          <button @click="showProfileModal = true; mobileOpen=false" class="flex items-center space-x-2 focus:outline-none">
            <img v-if="userStore.avatarUrl" :src="avatarSrc" alt="avatar" class="w-8 h-8 rounded-full object-cover border border-gray-300 dark:border-gray-600" />
            <div v-else class="w-8 h-8 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center text-gray-600 dark:text-gray-300 font-bold">{{ userInitials }}</div>
            <span class="font-medium text-gray-700 dark:text-gray-300">{{ userStore.displayName || 'Profile' }}</span>
          </button>
          <button @click="logout" class="bg-blue-600 dark:bg-blue-500 text-white px-3 py-1.5 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600">Logout</button>
        </div>
      </div>
    </div>
    
    <!-- Profile Modal (shared for both desktop and mobile) -->
    <UserProfileModal v-if="showProfileModal" :displayName="userStore.displayName" :avatarUrl="userStore.avatarUrl" @save="updateProfile" @close="showProfileModal = false" />
  </nav>
</template>

<script setup lang="ts">
import { useUserStore } from '../stores/user'
import { useAlertStore } from '../stores/alert'
import { useThemeStore } from '../stores/theme'
import { useRouter } from 'vue-router'
import { ref, computed } from 'vue'
import UserProfileModal from './UserProfileModal.vue'

const userStore = useUserStore()
const alertStore = useAlertStore()
const themeStore = useThemeStore()
const router = useRouter()
const showProfileModal = ref(false)
const mobileOpen = ref(false)

const logout = () => {
  userStore.logout()
  router.push('/login')
}

const userInitials = computed(() => {
  if (userStore.displayName) {
    return userStore.displayName.split(' ').map(n => n[0]).join('').toUpperCase()
  }
  if (userStore.email) {
    return userStore.email[0]?.toUpperCase() || 'U'
  }
  return 'U'
})

import { ref as vueRef } from 'vue'
const avatarUpdateKey = vueRef(Date.now())

const avatarSrc = computed(() => {
  if (!userStore.avatarUrl) return ''
  const base = userStore.avatarUrl.startsWith('http') ? '' : 'https://gearcrossroads-api.onrender.com'
  return base + userStore.avatarUrl + '?t=' + avatarUpdateKey.value
})

async function updateProfile({ displayName, avatar }: { displayName: string, avatar?: File|null }) {
  try {
    await userStore.updateProfile(displayName, avatar)
    avatarUpdateKey.value = Date.now()
    showProfileModal.value = false
  } catch (err: any) {
    const errorMessage = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.title || err.response?.data?.message || 'Failed to update profile.'
    alertStore.show(errorMessage, 'error')
  }
}
</script>

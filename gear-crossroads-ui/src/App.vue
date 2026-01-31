
<template>
  <div class="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900">
    <CustomAlert
      v-if="alert.visible"
      :message="alert.message"
      :duration="alert.duration"
      @close="alert.close"
    />
    <NavBar />
    <main class="flex-grow">
      <!-- TOGGLE: Comment out MaintenancePage and uncomment router-view to enable the site -->
      <MaintenancePage />
      <!-- <router-view /> -->
    </main>
    <footer class="bg-white dark:bg-gray-800 mt-auto py-8 px-6 border-t border-gray-200 dark:border-gray-700">
      <div class="max-w-7xl mx-auto text-center text-gray-500 dark:text-gray-400">
        <p>&copy; 2026 Aidan Fahey. All rights reserved.</p>
        <div class="mt-3 flex justify-center gap-6 items-center">
          <router-link 
            to="/terms-of-service" 
            class="text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 transition-colors text-sm"
          >
            Terms of Service
          </router-link>
          <router-link 
            to="/contact" 
            class="text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 transition-colors text-sm"
          >
            Contact Us
          </router-link>
          <a
            href="https://www.linkedin.com/in/aidanfahey/"
            target="_blank"
            rel="noopener noreferrer"
            class="text-gray-500 dark:text-gray-400 hover:text-blue-700 dark:hover:text-blue-400 transition-colors"
            aria-label="LinkedIn profile"
            title="LinkedIn"
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-7 h-7">
              <path d="M20.447 20.452h-3.554V14.86c0-1.335-.027-3.054-1.862-3.054-1.862 0-2.148 1.454-2.148 2.956v5.689H9.329V9h3.414v1.561h.049c.476-.9 1.637-1.85 3.37-1.85 3.602 0 4.268 2.371 4.268 5.455v6.286zM5.337 7.433a2.062 2.062 0 11.002-4.124 2.062 2.062 0 01-.002 4.124zM6.999 20.452H3.673V9h3.326v11.452zM22.225 0H1.771C.792 0 0 .77 0 1.72v20.56C0 23.23.792 24 1.771 24h20.451C23.2 24 24 23.23 24 22.28V1.72C24 .77 23.2 0 22.222 0h.003z"/>
            </svg>
          </a>
        </div>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import NavBar from './components/NavBar.vue'
import CustomAlert from './components/CustomAlert.vue'
import MaintenancePage from './components/MaintenancePage.vue'
import { useAlertStore } from './stores/alert'
import { useUserStore } from './stores/user'

const alert = useAlertStore()
const userStore = useUserStore()

// Check for expired token when app loads
onMounted(() => {
  if (userStore.token && userStore.isTokenExpired()) {
    userStore.logout()
    alert.show('Your session has expired. Please log in again.', 'error', 4000)
  }
})
</script>

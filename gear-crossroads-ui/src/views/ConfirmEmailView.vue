<template>
  <div class="pt-20">
    <div class="max-w-xl mx-auto bg-white dark:bg-gray-800 p-8 rounded-2xl shadow">
      <h1 class="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">Email Confirmation</h1>
      <div v-if="loading" class="text-gray-500 dark:text-gray-400">Confirming your email...</div>
      <div v-else>
        <p v-if="success" class="text-green-700 dark:text-green-400 mb-4">Your email has been confirmed successfully.</p>
        <p v-else class="text-red-700 dark:text-red-400 mb-4">We couldn't confirm your email. The link may be invalid or expired.</p>
        <router-link to="/login" class="text-blue-600 dark:text-blue-400 hover:underline">Go to Login</router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'

const route = useRoute()
const loading = ref(true)
const success = ref(false)

onMounted(async () => {
  try {
    const userId = route.query.userId as string
    const token = route.query.token as string
    if (!userId || !token) throw new Error('Missing token')
    await axios.get('/api/auth/confirm-email', { params: { userId, token } })
    success.value = true
  } catch {
    success.value = false
  } finally {
    loading.value = false
  }
})
</script>

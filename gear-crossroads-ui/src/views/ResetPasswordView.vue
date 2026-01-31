<template>
  <div class="pt-20">
    <div class="max-w-xl mx-auto bg-white dark:bg-gray-800 p-8 rounded-2xl shadow">
      <h1 class="text-2xl font-bold mb-4 text-gray-900 dark:text-gray-100">Reset Password</h1>
      <div v-if="!token || !email" class="text-red-700 dark:text-red-400">Invalid reset link.</div>
      <form v-else @submit.prevent="submit" class="flex flex-col gap-4">
        <input v-model="password" type="password" placeholder="New password" class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" required />
        <input v-model="confirm" type="password" placeholder="Confirm password" class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" required />
        <button type="submit" class="bg-blue-600 dark:bg-blue-700 text-white px-4 py-2 rounded hover:bg-blue-700 dark:hover:bg-blue-800 transition-colors" :disabled="loading">
          {{ loading ? 'Resetting...' : 'Reset Password' }}
        </button>
        <p v-if="message" :class="{ 'text-green-700 dark:text-green-400': success, 'text-red-700 dark:text-red-400': !success }">{{ message }}</p>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'

const route = useRoute()
const email = computed(() => route.query.email as string || '')
const token = computed(() => route.query.token as string || '')
const password = ref('')
const confirm = ref('')
const loading = ref(false)
const message = ref('')
const success = ref(false)

const submit = async () => {
  if (password.value !== confirm.value) {
    message.value = 'Passwords do not match.'
    success.value = false
    return
  }
  loading.value = true
  try {
    await axios.post('/api/auth/reset-password', {
      email: email.value,
      token: token.value,
      newPassword: password.value
    })
    message.value = 'Password reset successfully. You can now log in.'
    success.value = true
  } catch (e: any) {
    message.value = Array.isArray(e?.response?.data) ? e.response.data.join(', ') : 'Failed to reset password.'
    success.value = false
  } finally {
    loading.value = false
  }
}
</script>

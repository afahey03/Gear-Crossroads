<template>
  <div class="pt-20">
    <div class="max-w-xl mx-auto bg-white p-8 rounded-2xl shadow">
      <h1 class="text-2xl font-bold mb-4">Request Password Reset</h1>
      <form @submit.prevent="submit" class="flex flex-col gap-4">
        <input v-model="email" type="email" placeholder="Your email" class="border rounded p-2" required />
        <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded" :disabled="loading">
          {{ loading ? 'Sending...' : 'Send reset link' }}
        </button>
        <p v-if="message" :class="{ 'text-green-700': success, 'text-red-700': !success }">{{ message }}</p>
        <p class="text-sm text-gray-600">If an account exists with that email, you'll receive a reset link.</p>
      </form>
    </div>
  </div>
  
</template>

<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'

const email = ref('')
const loading = ref(false)
const message = ref('')
const success = ref(false)

const submit = async () => {
  loading.value = true
  message.value = ''
  try {
    await axios.post('/api/auth/request-password-reset', { email: email.value })
    message.value = 'If an account exists, a reset link has been sent.'
    success.value = true
  } catch (e: any) {
    // Do not reveal whether the email exists; show generic error
    message.value = 'If an account exists, a reset link has been sent.'
    success.value = true
  } finally {
    loading.value = false
  }
}
</script>

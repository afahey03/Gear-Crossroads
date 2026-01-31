<template>
  <div class="pt-20">
    <div class="max-w-2xl mx-auto p-6 bg-white dark:bg-gray-800 rounded-2xl shadow-md dark:shadow-gray-900 my-10">
      <h1 class="text-4xl font-bold mb-6 text-gray-900 dark:text-gray-100">Contact Support</h1>
      <p class="text-gray-600 dark:text-gray-400 mb-8">
        Have an issue or question? Fill out the form below and our support team will get back to you as soon as possible.
      </p>

      <form @submit.prevent="submitForm" class="space-y-6">
        <!-- Email Input -->
        <div>
          <label for="email" class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Your Email Address <span class="text-red-600 dark:text-red-400">*</span>
          </label>
          <input
            id="email"
            v-model="formData.email"
            type="email"
            required
            placeholder="your.email@example.com"
            class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-500 dark:placeholder-gray-400"
            :disabled="isSubmitting"
          />
        </div>

        <!-- Issue Type Dropdown -->
        <div>
          <label for="issueType" class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Type of Issue <span class="text-red-600 dark:text-red-400">*</span>
          </label>
          <select
            id="issueType"
            v-model="formData.issueType"
            required
            class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            :disabled="isSubmitting"
          >
            <option value="" disabled>Select an issue type</option>
            <option value="inappropriate-content">Report Inappropriate Content</option>
            <option value="harassment">Report Harassment or Discrimination</option>
            <option value="technical-issue">Technical Issue</option>
            <option value="account-issue">Account Issue</option>
            <option value="bug-report">Bug Report</option>
            <option value="feature-request">Feature Request</option>
            <option value="other">Other</option>
          </select>
        </div>

        <!-- Description Text Area -->
        <div>
          <label for="description" class="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
            Issue Description <span class="text-red-600 dark:text-red-400">*</span>
          </label>
          <textarea
            id="description"
            v-model="formData.description"
            required
            maxlength="2000"
            rows="8"
            placeholder="Please describe your issue in detail..."
            class="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-500 dark:placeholder-gray-400"
            :disabled="isSubmitting"
          ></textarea>
          <div class="text-sm text-gray-500 dark:text-gray-400 mt-1 text-right">
            {{ formData.description.length }} / 2000 characters
          </div>
        </div>

        <!-- Submit Button -->
        <div class="flex items-center justify-between pt-4">
          <router-link 
            to="/" 
            class="text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 font-medium"
            :class="{ 'pointer-events-none opacity-50': isSubmitting }"
          >
            ← Back to Home
          </router-link>
          <button
            type="submit"
            :disabled="isSubmitting || !isFormValid"
            class="bg-blue-600 dark:bg-blue-500 text-white px-8 py-3 rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:cursor-not-allowed font-semibold transition"
          >
            {{ isSubmitting ? 'Sending...' : 'Submit' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAlertStore } from '../stores/alert'

const router = useRouter()
const alertStore = useAlertStore()

const formData = ref({
  email: '',
  issueType: '',
  description: ''
})

const isSubmitting = ref(false)

const isFormValid = computed(() => {
  return (
    formData.value.email.trim() !== '' &&
    formData.value.issueType !== '' &&
    formData.value.description.trim() !== '' &&
    formData.value.description.length <= 2000
  )
})

async function submitForm() {
  if (!isFormValid.value || isSubmitting.value) return

  isSubmitting.value = true

  try {
    const response = await fetch('https://gearcrossroads-api.onrender.com/api/contact/submit', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(formData.value)
    })

    if (!response.ok) {
      throw new Error('Failed to submit contact form')
    }

    alertStore.show('Your message has been sent successfully! We will get back to you soon.', 'success', 3000)
    
    // Reset form
    formData.value = {
      email: '',
      issueType: '',
      description: ''
    }

    // Redirect to home after short delay
    setTimeout(() => {
      router.push('/')
    }, 2000)
  } catch (error) {
    console.error('Error submitting contact form:', error)
    alertStore.show('Failed to send message. Please try again later.', 'error')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
/* Form-specific styles if needed */
</style>

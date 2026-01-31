<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
    <div class="bg-white dark:bg-gray-800 p-10 rounded-2xl shadow-lg w-full max-w-md">
      <h1 class="text-3xl font-bold mb-6 text-center text-gray-900 dark:text-gray-100">Register</h1>
      <form @submit.prevent="submit" class="space-y-4">
        <input v-model="email" type="email" placeholder="Email"
               class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500 dark:focus:ring-green-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" />
        <input v-model="password" type="password" placeholder="Password"
               class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500 dark:focus:ring-green-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" />
        <button type="submit"
                class="w-full bg-green-500 dark:bg-green-600 text-white p-3 rounded-lg hover:bg-green-600 dark:hover:bg-green-700 transition-colors">
          Register
        </button>
      </form>
      <p v-if="error" class="text-red-500 dark:text-red-400 mt-2 text-center">{{ error }}</p>
      <p class="mt-4 text-center text-gray-600 dark:text-gray-400">
        Already have an account?
        <router-link to="/login" class="text-green-500 dark:text-green-400 hover:underline">Login</router-link>
      </p>
    </div>
  </div>
</template>

<script lang="ts">

import { ref } from 'vue';
import axios from 'axios';
import { useAlertStore } from '../stores/alert';

export default {
  setup() {
    const email = ref('');
    const password = ref('');
    const error = ref('');
    const alertStore = useAlertStore();

    const submit = async () => {
      try {
        error.value = '';
        await axios.post('/api/auth/register', { email: email.value, password: password.value });
        alertStore.show('Registration successful. Please check your email to confirm your account before logging in.', 'success');
      } catch (err: any) {
        const errorData = err.response?.data;
        // Check if the error is about duplicate email/username
        if (Array.isArray(errorData)) {
          const errorString = errorData.join('; ').toLowerCase();
          if (errorString.includes('already taken') || errorString.includes('is already taken')) {
            error.value = 'An account with this email already exists.';
          } else {
            error.value = errorData.join('; ');
          }
        } else {
          error.value = errorData || 'Registration failed';
        }
        // Don't show alert for duplicate email error
        if (!error.value.includes('already exists')) {
          alertStore.show(error.value, 'error');
        }
      }
    };

    return { email, password, submit, error };
  }
};
</script>

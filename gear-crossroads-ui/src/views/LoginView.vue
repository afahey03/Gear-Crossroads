<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
    <div class="bg-white dark:bg-gray-800 p-10 rounded-2xl shadow-lg w-full max-w-md">
      <h1 class="text-3xl font-bold mb-6 text-center text-gray-900 dark:text-gray-100">Login</h1>
      <form @submit.prevent="submit" class="space-y-4">
        <input v-model="email" type="email" placeholder="Email"
               class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" />
        <input v-model="password" type="password" placeholder="Password"
               class="w-full p-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500" />
        <button type="submit"
                class="w-full bg-blue-500 dark:bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-600 dark:hover:bg-blue-700 transition-colors">
          Login
        </button>
      </form>
      <p v-if="error" class="text-red-500 dark:text-red-400 mt-2 text-center">{{ error }}</p>
      <p class="mt-4 text-center text-gray-600 dark:text-gray-400">
        Don't have an account?
        <router-link to="/register" class="text-blue-500 dark:text-blue-400 hover:underline">Register</router-link>
      </p>
      <p class="mt-2 text-center">
        <router-link to="/request-password-reset" class="text-sm text-blue-500 dark:text-blue-400 hover:underline">Forgot your password?</router-link>
      </p>
    </div>
  </div>
</template>

<script lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '../stores/user';

export default {
  setup() {
    const email = ref('');
    const password = ref('');
    const error = ref('');
    const router = useRouter();
    const userStore = useUserStore();

    const submit = async () => {
      try {
        error.value = '';
        await userStore.login(email.value, password.value);
        router.push('/');
      } catch (err: any) {
        console.log('Login error caught:', err);
        console.log('Error response status:', err.response?.status);
        console.log('Error response data:', err.response?.data);
        
        const errorData = err.response?.data;
        
        // Show user-friendly error message for invalid credentials
        if (typeof errorData === 'string' && errorData.toLowerCase().includes('invalid')) {
          error.value = 'The email or password are incorrect.';
        } else if (errorData?.message?.toLowerCase().includes('invalid')) {
          error.value = 'The email or password are incorrect.';
        } else if (typeof errorData === 'string' && errorData.toLowerCase().includes('not confirmed')) {
          error.value = errorData;
        } else if (typeof errorData === 'string' && errorData.toLowerCase().includes('banned')) {
          error.value = errorData;
        } else {
          error.value = typeof errorData === 'string' ? errorData : (errorData?.message || 'Login failed');
        }
        
        console.log('Error value set to:', error.value);
      }
    };

    return { email, password, submit, error };
  }
};
</script>

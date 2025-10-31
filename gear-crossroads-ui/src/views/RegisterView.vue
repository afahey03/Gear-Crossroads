<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="bg-white p-10 rounded-2xl shadow-lg w-full max-w-md">
      <h1 class="text-3xl font-bold mb-6 text-center">Register</h1>
      <form @submit.prevent="submit" class="space-y-4">
        <input v-model="email" type="email" placeholder="Email"
               class="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500" />
        <input v-model="password" type="password" placeholder="Password"
               class="w-full p-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500" />
        <button type="submit"
                class="w-full bg-green-500 text-white p-3 rounded-lg hover:bg-green-600 transition-colors">
          Register
        </button>
      </form>
      <p v-if="error" class="text-red-500 mt-2 text-center">{{ error }}</p>
      <p class="mt-4 text-center text-gray-600">
        Already have an account?
        <router-link to="/login" class="text-green-500 hover:underline">Login</router-link>
      </p>
    </div>
  </div>
</template>

<script lang="ts">
import { ref } from 'vue';
import axios from 'axios';
import { useRouter } from 'vue-router';

export default {
  setup() {
    const email = ref('');
    const password = ref('');
    const error = ref('');
    const router = useRouter();

    const submit = async () => {
      try {
        error.value = '';
        await axios.post('/api/auth/register', { email: email.value, password: password.value });
        alert('Registration successful! You can now log in.');
        router.push('/login');
      } catch (err: any) {
        error.value = Array.isArray(err.response?.data) ? err.response.data.join('; ') : 'Registration failed';
      }
    };

    return { email, password, submit, error };
  }
};
</script>

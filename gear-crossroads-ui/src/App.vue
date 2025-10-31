<template>
  <div class="min-h-screen flex flex-col bg-gray-50">
    <nav class="bg-white shadow-md sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
        <router-link to="/" class="text-2xl font-bold text-blue-600 hover:text-blue-700 transition-colors">
          Gear Crossroads
        </router-link>
        <div class="space-x-6 flex items-center">
          <router-link to="/" class="text-gray-700 hover:text-blue-600 transition-colors">Home</router-link>
          <router-link 
            v-if="!userStore.token" 
            to="/login" 
            class="text-gray-700 hover:text-blue-600 transition-colors">
            Login
          </router-link>
          <router-link 
            v-if="!userStore.token" 
            to="/register" 
            class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
            Register
          </router-link>
          <button 
            v-if="userStore.token" 
            @click="logout" 
            class="text-gray-700 hover:text-red-500 transition-colors">
            Logout
          </button>
        </div>
      </div>
    </nav>

    <main class="flex-grow">
      <router-view />
    </main>

    <footer class="bg-white mt-auto py-8 px-6">
      <div class="max-w-7xl mx-auto text-center text-gray-500">
        <p>&copy; 2025 Gear Crossroads. All rights reserved.</p>
      </div>
    </footer>
  </div>
</template>

<script lang="ts">
import { useUserStore } from './stores/user';
import { useRouter } from 'vue-router';

export default {
  setup() {
    const userStore = useUserStore();
    const router = useRouter();
    const logout = () => {
      userStore.logout();
      router.push('/');
    };
    return { userStore, logout };
  }
};
</script>

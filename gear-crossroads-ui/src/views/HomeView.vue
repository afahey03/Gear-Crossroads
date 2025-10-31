<template>
  <div>
    <!-- Hero Section -->
    <section class="bg-gradient-to-r from-blue-600 to-blue-500 text-white py-24 px-6 text-center">
      <h1 class="text-4xl md:text-5xl font-bold mb-4">Discover the Gear People Love</h1>
      <p class="text-lg md:text-xl mb-8">Explore setups from other enthusiasts, get inspired, and share your own!</p>
      <router-link to="/register"
                   class="bg-white text-blue-600 font-semibold px-6 py-3 rounded-lg shadow hover:bg-gray-100 transition">
        Get Started
      </router-link>
    </section>

    <!-- Setups Grid -->
    <section class="max-w-7xl mx-auto py-12 px-6">
      <h2 class="text-3xl font-bold mb-8 text-center">Popular Setups</h2>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-8">
        <SetupCard title="WFH Setup" description="My work from home setup" />
        <SetupCard title="Gaming Rig" description="PC gaming rig" />
        <SetupCard title="Photography Kit" description="My camera setup" />
      </div>
    </section>
  </div>
</template>

<script lang="ts">
import SetupCard from '../components/SetupCard.vue';
export default {
  components: { SetupCard }
};
</script>

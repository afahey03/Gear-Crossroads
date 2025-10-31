<template>
  <nav>
    <router-link to="/">Home</router-link> |
    <router-link to="/login">Login</router-link>
  </nav>
  <router-view />
</template>

<script lang="ts">
export default {};
</script>

<style scoped>
nav {
  padding: 1rem;
  background: #f5f5f5;
  margin-bottom: 1rem;
}
nav a {
  margin-right: 1rem;
  text-decoration: none;
  color: #333;
}
nav a.router-link-active {
  font-weight: bold;
  color: #007bff;
}
</style>

<template>
  <div>
    <h1>Login</h1>
    <form @submit.prevent="submit">
      <input v-model="email" type="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password" />
      <button type="submit">Login</button>
    </form>
  </div>
</template>

<script lang="ts">
import { ref } from 'vue';
import { useUserStore } from '../stores/user';

export default {
  setup() {
    const userStore = useUserStore();
    const email = ref('');
    const password = ref('');

    const submit = async () => {
      try {
        await userStore.login(email.value, password.value);
        alert('Logged in!');
      } catch (err) {
        console.error(err);
        alert('Login failed');
      }
    };

    return { email, password, submit };
  }
};
</script>

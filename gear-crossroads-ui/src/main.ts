import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import { createPinia } from 'pinia';
import axios from 'axios';

// Axios defaults
axios.defaults.baseURL = 'https://localhost:5001';
axios.defaults.headers.common['Content-Type'] = 'application/json';

const app = createApp(App);
app.use(router);
app.use(createPinia());
app.mount('#app');

import { createApp } from 'vue';
import App from './App.vue';
import router from './router';
import { createPinia } from 'pinia';
import axios from 'axios';
import './index.css';

axios.defaults.baseURL = 'https://localhost:7209';
axios.defaults.headers.common['Content-Type'] = 'application/json';

const app = createApp(App);
app.use(router);
app.use(createPinia());
app.mount('#app');

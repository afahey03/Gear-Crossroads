import { createApp, watch } from 'vue';
import App from './App.vue';
import router from './router';
import { createPinia } from 'pinia';
import axios from 'axios';
import { useUserStore } from './stores/user';
import { useThemeStore } from './stores/theme';
import './index.css';

axios.defaults.baseURL = 'https://gearcrossroads-api.onrender.com';
axios.defaults.headers.common['Content-Type'] = 'application/json';

const app = createApp(App);

const pinia = createPinia();
app.use(pinia);

const userStore = useUserStore();

watch(
    () => userStore.token,
    (token) => {
        if (token) {
            axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
            console.debug('[Axios] JWT token attached:', token.substring(0, 20) + '...');
        } else {
            delete axios.defaults.headers.common['Authorization'];
            console.debug('[Axios] JWT token removed');
        }
    },
    { immediate: true }
);

// Initialize theme
const themeStore = useThemeStore();
themeStore.initializeTheme();

app.use(router);

app.mount('#app');

// Global error handler: redirect to error page on unexpected server errors
let isRefreshing = false;
let pendingQueue: Array<{ resolve: (token: string) => void; reject: (err: any) => void }> = [];

function getCookie(name: string): string | null {
    const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    return match && match[2] ? decodeURIComponent(match[2] as string) : null;
}

async function refreshAccessToken() {
    if (isRefreshing) {
        return new Promise<string>((resolve, reject) => pendingQueue.push({ resolve, reject }));
    }
    isRefreshing = true;
    try {
        const csrf = getCookie('gc-csrf') || '';
        const res = await axios.post('/api/auth/refresh', {}, { withCredentials: true, headers: { 'X-CSRF': csrf } });
        const newToken: string = res.data?.token;
        if (newToken) {
            userStore.setToken(newToken);
            pendingQueue.forEach(p => p.resolve(newToken));
            return newToken;
        }
        const err = new Error('No token from refresh');
        pendingQueue.forEach(p => p.reject(err));
        throw err;
    } catch (e) {
        pendingQueue.forEach(p => p.reject(e));
        throw e;
    } finally {
        pendingQueue = [];
        isRefreshing = false;
    }
}

axios.interceptors.response.use(
    (response) => response,
    async (error) => {
        const status = error?.response?.status;
        const original = error?.config;

        // Don't attempt refresh for login/register endpoints
        const isAuthEndpoint = original?.url?.includes('/api/auth/login') ||
            original?.url?.includes('/api/auth/register') ||
            original?.url?.includes('/api/auth/refresh');

        if (status === 401 && original && !original.__isRetryRequest && !isAuthEndpoint) {
            try {
                const newToken = await refreshAccessToken();
                original.__isRetryRequest = true;
                original.headers = original.headers || {};
                original.headers['Authorization'] = `Bearer ${newToken}`;
                return axios(original);
            } catch {
                // hard logout and go to login
                userStore.logout();
                if (router.currentRoute.value.path !== '/login') router.push('/login');
                return Promise.reject(error);
            }
        }
        const currentPath = router.currentRoute.value.path;
        if ((status >= 500 || !status) && currentPath !== '/error') {
            router.push('/error');
            return Promise.resolve({ data: null });
        }
        return Promise.reject(error);
    }
);

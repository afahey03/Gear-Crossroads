import { defineStore } from 'pinia';
import axios from 'axios';

interface UserState {
    token: string;
    user: any | null;
}

export const useUserStore = defineStore('user', {
    state: (): UserState => ({
        token: localStorage.getItem('token') || '',
        user: null,
    }),
    actions: {
        async login(email: string, password: string) {
            const response = await axios.post('https://localhost:5001/api/auth/login', { email, password });
            this.token = response.data.token;
            localStorage.setItem('token', this.token);
        },
        logout() {
            this.token = '';
            this.user = null;
            localStorage.removeItem('token');
        },
    },
});

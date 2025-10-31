import { defineStore } from 'pinia';
import axios from 'axios';

interface UserState {
    token: string;
    email: string;
}

export const useUserStore = defineStore('user', {
    state: (): UserState => ({
        token: localStorage.getItem('token') || '',
        email: ''
    }),
    actions: {
        setToken(token: string) {
            this.token = token;
            localStorage.setItem('token', token);
            axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        },
        logout() {
            this.token = '';
            this.email = '';
            localStorage.removeItem('token');
            delete axios.defaults.headers.common['Authorization'];
        }
    }
});

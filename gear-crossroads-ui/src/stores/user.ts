import { defineStore } from 'pinia'
import axios from 'axios'

export const useUserStore = defineStore('user', {
    state: () => ({
        token: localStorage.getItem('token') || '',
        displayName: localStorage.getItem('displayName') || '',
        avatarUrl: localStorage.getItem('avatarUrl') || '',
        email: localStorage.getItem('email') || '',
        userId: localStorage.getItem('userId') || '',
        isAdmin: localStorage.getItem('isAdmin') === 'true' || false
    }),
    getters: {
        isLoggedIn: (state) => !!state.token && state.token.length > 0
    },
    actions: {
        setToken(token: string) {
            this.token = token
            localStorage.setItem('token', token)
            axios.defaults.headers.common['Authorization'] = `Bearer ${token}`

            // Decode JWT to check for admin claim
            if (token) {
                try {
                    const parts = token.split('.')
                    if (parts.length === 3 && parts[1]) {
                        const payload = JSON.parse(atob(parts[1]))
                        this.isAdmin = payload.admin === 'true' || payload.role === 'Admin'
                        localStorage.setItem('isAdmin', this.isAdmin.toString())
                    }
                } catch {
                    this.isAdmin = false
                }
            }
        },

        isTokenExpired(): boolean {
            if (!this.token) return true

            try {
                const parts = this.token.split('.')
                if (parts.length !== 3 || !parts[1]) return true

                const payload = JSON.parse(atob(parts[1]))

                // JWT exp is in seconds, Date.now() is in milliseconds
                if (!payload.exp) return true

                const expirationTime = payload.exp * 1000
                const currentTime = Date.now()

                return currentTime >= expirationTime
            } catch (error) {
                console.error('Error checking token expiration:', error)
                return true
            }
        },

        checkAndHandleExpiredToken(): boolean {
            if (this.token && this.isTokenExpired()) {
                console.log('Token has expired, logging out user')
                this.logout()
                return true
            }
            return false
        },

        setProfile(profile: { displayName: string, avatarUrl: string, email: string, id?: string }) {
            this.displayName = profile.displayName
            this.avatarUrl = profile.avatarUrl
            this.email = profile.email
            if (profile.id) {
                this.userId = profile.id
                localStorage.setItem('userId', profile.id)
            }
            localStorage.setItem('displayName', profile.displayName)
            localStorage.setItem('avatarUrl', profile.avatarUrl)
            localStorage.setItem('email', profile.email)
        },

        async fetchProfile() {
            if (!this.token) return
            const res = await axios.get('/api/auth/profile')
            this.setProfile(res.data)
        },

        async login(email: string, password: string) {
            const res = await axios.post('/api/auth/login', { email, password }, { withCredentials: true })
            this.setToken(res.data.token)
            await this.fetchProfile()
        },


        async updateProfile(displayName: string, avatar?: File | null) {
            if (!this.token) return
            const form = new FormData()
            form.append('displayName', displayName)
            if (avatar) form.append('avatar', avatar)
            const res = await axios.put('/api/auth/profile', form, {
                headers: { 'Content-Type': 'multipart/form-data' }
            })
            this.setProfile(res.data)
        },

        async deleteAccount() {
            if (!this.token) return
            try {
                const csrf = (document.cookie.match(/(?:^|; )gc-csrf=([^;]*)/) || [])[1]
                await axios.delete('/api/auth/account', {
                    withCredentials: true,
                    headers: csrf ? { 'X-CSRF': decodeURIComponent(csrf) } : {}
                })
                // Clear all user data after successful deletion
                this.logout()
            } catch (error) {
                throw error
            }
        },

        async logout() {
            try {
                const csrf = (document.cookie.match(/(?:^|; )gc-csrf=([^;]*)/) || [])[1]
                await axios.post('/api/auth/logout', {}, { withCredentials: true, headers: csrf ? { 'X-CSRF': decodeURIComponent(csrf) } : {} })
            } catch { }
            this.token = ''
            this.displayName = ''
            this.avatarUrl = ''
            this.email = ''
            this.userId = ''
            this.isAdmin = false
            localStorage.removeItem('token')
            localStorage.removeItem('displayName')
            localStorage.removeItem('avatarUrl')
            localStorage.removeItem('email')
            localStorage.removeItem('userId')
            localStorage.removeItem('isAdmin')
            delete axios.defaults.headers.common['Authorization']
        }
    }
})

import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export const useThemeStore = defineStore('theme', () => {
    const isDark = ref(false)

    // Initialize from localStorage or system preference
    const initializeTheme = () => {
        const stored = localStorage.getItem('theme')
        if (stored) {
            isDark.value = stored === 'dark'
        } else {
            // Check system preference
            isDark.value = window.matchMedia('(prefers-color-scheme: dark)').matches
        }
        applyTheme()
    }

    // Apply theme to document
    const applyTheme = () => {
        if (isDark.value) {
            document.documentElement.classList.add('dark')
        } else {
            document.documentElement.classList.remove('dark')
        }
    }

    // Toggle theme
    const toggleTheme = () => {
        isDark.value = !isDark.value
        localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
        applyTheme()
    }

    // Watch for changes
    watch(isDark, () => {
        applyTheme()
    })

    return {
        isDark,
        toggleTheme,
        initializeTheme
    }
})

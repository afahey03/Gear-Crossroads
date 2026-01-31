import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAlertStore = defineStore('alert', () => {
    const message = ref('')
    const visible = ref(false)
    const type = ref<'info' | 'success' | 'error'>('info')
    const duration = ref(2500)

    function show(msg: string, alertType: 'info' | 'success' | 'error' = 'info', time?: number) {
        message.value = msg
        type.value = alertType
        visible.value = true
        duration.value = time ?? 2500
    }

    function close() {
        visible.value = false
    }

    return { message, visible, type, duration, show, close }
})
